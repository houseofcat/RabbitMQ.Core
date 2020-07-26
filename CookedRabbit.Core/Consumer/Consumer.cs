using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using CookedRabbit.Core.WorkEngines;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CookedRabbit.Core
{
    public interface IConsumer<TFromQueue>
    {
        IChannelPool ChannelPool { get; }
        Channel<TFromQueue> DataBuffer { get; }
        Config Config { get; }
        ConsumerOptions ConsumerSettings { get; }
        bool Started { get; }

        ReadOnlyMemory<byte> HashKey { get; set; }

        Task DataflowExecutionEngineAsync(Func<TFromQueue, Task<bool>> workBodyAsync, int maxDoP = 4, CancellationToken token = default);
        Task PipelineExecutionEngineAsync<TLocalOut>(IPipeline<TFromQueue, TLocalOut> pipeline, bool waitForCompletion, CancellationToken token = default);
        Task PipelineStreamEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default);

        ChannelReader<TFromQueue> GetConsumerBuffer();
        ValueTask<TFromQueue> ReadAsync();
        Task<IEnumerable<TFromQueue>> ReadUntilEmptyAsync();
        Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true);
        Task StopConsumerAsync(bool immediate = false);
        IAsyncEnumerable<TFromQueue> StreamOutUntilClosedAsync();
        IAsyncEnumerable<TFromQueue> StreamOutUntilEmptyAsync();
    }

    public class Consumer : IConsumer<ReceivedData>
    {
        private ILogger<Consumer> _logger;
        private readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);

        public Config Config { get; }

        public IChannelPool ChannelPool { get; }
        public Channel<ReceivedData> DataBuffer { get; set; }

        private IChannelHost ConsumingChannelHost { get; set; }

        public ConsumerOptions ConsumerSettings { get; private set; }

        public bool Started { get; private set; }
        private bool Shutdown { get; set; }

        public ReadOnlyMemory<byte> HashKey { get; set; }

        public Consumer(Config config, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(config, nameof(config));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            _logger = LogHelper.GetLogger<Consumer>();
            Config = config;
            ChannelPool = new ChannelPool(Config);
            HashKey = hashKey;

            ConsumerSettings = Config.GetConsumerSettings(consumerName);
        }

        public Consumer(IChannelPool channelPool, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            _logger = LogHelper.GetLogger<Consumer>();
            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

            ConsumerSettings = Config.GetConsumerSettings(consumerName);
        }

        public Consumer(IChannelPool channelPool, ConsumerOptions consumerSettings, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerSettings, nameof(consumerSettings));

            _logger = LogHelper.GetLogger<Consumer>();
            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;
            ConsumerSettings = consumerSettings;
        }

        public async Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true)
        {
            await _conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (ConsumerSettings.Enabled && !Started)
                {
                    Shutdown = false;
                    DataBuffer = Channel.CreateBounded<ReceivedData>(
                    new BoundedChannelOptions(ConsumerSettings.BatchSize.Value)
                    {
                        FullMode = ConsumerSettings.BehaviorWhenFull.Value
                    });

                    await DataBuffer
                        .Writer
                        .WaitToWriteAsync()
                        .ConfigureAwait(false);

                    bool success;
                    do
                    {
                        _logger.LogTrace(LogMessages.Consumer.StartingConsumerLoop, ConsumerSettings.ConsumerName);
                        success = await StartConsumingAsync().ConfigureAwait(false);
                    }
                    while (!success);

                    _logger.LogDebug(LogMessages.Consumer.Started, ConsumerSettings.ConsumerName);

                    Started = true;
                }
            }
            finally { _conLock.Release(); }
        }

        public async Task StopConsumerAsync(bool immediate = false)
        {
            await _conLock
                .WaitAsync()
                .ConfigureAwait(false);

            _logger.LogDebug(LogMessages.Consumer.StopConsumer, ConsumerSettings.ConsumerName);

            try
            {
                if (Started)
                {
                    Shutdown = true;
                    DataBuffer.Writer.Complete();

                    if (immediate)
                    {
                        ConsumingChannelHost.Close();
                    }

                    await DataBuffer
                        .Reader
                        .Completion
                        .ConfigureAwait(false);

                    Started = false;
                    _logger.LogDebug(
                        LogMessages.Consumer.StoppedConsumer,
                        ConsumerSettings.ConsumerName);
                }
            }
            finally { _conLock.Release(); }
        }

        private async Task<bool> StartConsumingAsync()
        {
            if (Shutdown)
            { return false; }

            _logger.LogInformation(
                LogMessages.Consumer.StartingConsumer,
                ConsumerSettings.ConsumerName);

            await SetChannelHostAsync()
                .ConfigureAwait(false);

            if (ConsumingChannelHost == null) { return false; }

            if (Config.FactorySettings.EnableDispatchConsumersAsync)
            {
                var consumer = CreateAsyncConsumer();
                if (consumer == null) { return false; }

                ConsumingChannelHost
                    .Channel
                    .BasicConsume(
                        ConsumerSettings.QueueName,
                        ConsumerSettings.AutoAck ?? false,
                        ConsumerSettings.ConsumerName,
                        ConsumerSettings.NoLocal ?? false,
                        ConsumerSettings.Exclusive ?? false,
                        null,
                        consumer);
            }
            else
            {
                var consumer = CreateConsumer();
                if (consumer == null) { return false; }

                ConsumingChannelHost
                    .Channel
                    .BasicConsume(
                        ConsumerSettings.QueueName,
                        ConsumerSettings.AutoAck ?? false,
                        ConsumerSettings.ConsumerName,
                        ConsumerSettings.NoLocal ?? false,
                        ConsumerSettings.Exclusive ?? false,
                        null,
                        consumer);
            }

            _logger.LogInformation(
                LogMessages.Consumer.StartedConsumer,
                ConsumerSettings.ConsumerName);

            return true;
        }

        private async Task SetChannelHostAsync()
        {
            try
            {
                if (ConsumerSettings.UseTransientChannels ?? true)
                {
                    var autoAck = ConsumerSettings.AutoAck ?? false;
                    _logger.LogTrace(LogMessages.Consumer.GettingTransientChannelHost, ConsumerSettings.ConsumerName);
                    ConsumingChannelHost = await ChannelPool
                        .GetTransientChannelAsync(!autoAck)
                        .ConfigureAwait(false);
                }
                else if (ConsumerSettings.AutoAck ?? false)
                {
                    _logger.LogTrace(LogMessages.Consumer.GettingChannelHost, ConsumerSettings.ConsumerName);
                    ConsumingChannelHost = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);
                }
                else
                {
                    _logger.LogTrace(LogMessages.Consumer.GettingAckChannelHost, ConsumerSettings.ConsumerName);
                    ConsumingChannelHost = await ChannelPool
                        .GetAckChannelAsync()
                        .ConfigureAwait(false);
                }
            }
            catch { ConsumingChannelHost = null; }

            if (ConsumingChannelHost != null)
            {
                _logger.LogDebug(
                    LogMessages.Consumer.ChannelEstablished,
                    ConsumerSettings.ConsumerName,
                    ConsumingChannelHost?.ChannelId ?? 0ul);
            }
            else
            {
                _logger.LogError(
                    LogMessages.Consumer.ChannelNotEstablished,
                    ConsumerSettings.ConsumerName);
            }

        }

        private EventingBasicConsumer CreateConsumer()
        {
            EventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.Channel.BasicQos(0, ConsumerSettings.BatchSize.Value, false);
                consumer = new EventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandler;
                consumer.Shutdown += ConsumerShutdown;
            }
            catch { }

            return consumer;
        }

        private async void ReceiveHandler(object o, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(ConsumingChannelHost.Channel, bdea, !ConsumerSettings.AutoAck.Value, HashKey);

            _logger.LogDebug(
                LogMessages.Consumer.ConsumerMessageReceived,
                ConsumerSettings.ConsumerName,
                bdea.DeliveryTag);

            if (await DataBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false))
            {
                await DataBuffer
                    .Writer
                    .WriteAsync(rabbitMessage);
            }
        }

        private async void ConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            await HandleUnexpectedShutdownAsync();
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            AsyncEventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.Channel.BasicQos(0, ConsumerSettings.BatchSize.Value, false);
                consumer = new AsyncEventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandlerAsync;
                consumer.Shutdown += ConsumerShutdownAsync;
            }
            catch { }

            return consumer;
        }

        private async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(ConsumingChannelHost.Channel, bdea, !ConsumerSettings.AutoAck.Value, HashKey);

            _logger.LogDebug(
                LogMessages.Consumer.ConsumerAsyncMessageReceived,
                ConsumerSettings.ConsumerName,
                bdea.DeliveryTag);

            if (await DataBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                await DataBuffer
                    .Writer
                    .WriteAsync(rabbitMessage);
            }
        }

        private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
        {
            await HandleUnexpectedShutdownAsync();
        }

        private async Task HandleUnexpectedShutdownAsync()
        {
            if (!Shutdown)
            {
                bool success;
                do
                {
                    _logger.LogWarning(
                        LogMessages.Consumer.ConsumerShutdownEvent,
                        ConsumerSettings.ConsumerName);

                    success = await StartConsumingAsync().ConfigureAwait(false);
                }
                while (!Shutdown && !success);
            }
        }

        public ChannelReader<ReceivedData> GetConsumerBuffer() => DataBuffer.Reader;

        public async ValueTask<ReceivedData> ReadAsync()
        {
            if (!await DataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            return await DataBuffer
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReceivedData>> ReadUntilEmptyAsync()
        {
            if (!await DataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            var list = new List<ReceivedData>();
            await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (DataBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                list.Add(message);
            }

            return list;
        }

        public async IAsyncEnumerable<ReceivedData> StreamOutUntilEmptyAsync()
        {
            if (!await DataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (DataBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                yield return message;
            }
        }

        public async IAsyncEnumerable<ReceivedData> StreamOutUntilClosedAsync()
        {
            if (!await DataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            await foreach (var receivedData in DataBuffer.Reader.ReadAllAsync())
            {
                yield return receivedData;
            }
        }

        private readonly SemaphoreSlim dataFlowExecLock = new SemaphoreSlim(1, 1);

        public async Task DataflowExecutionEngineAsync(Func<ReceivedData, Task<bool>> workBodyAsync, int maxDoP = 4, CancellationToken token = default)
        {
            await dataFlowExecLock.WaitAsync(2000).ConfigureAwait(false);

            try
            {

                var dataflowEngine = new DataflowEngine(workBodyAsync, maxDoP);

                while (await DataBuffer.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (DataBuffer.Reader.TryRead(out var receivedData))
                    {
                        if (receivedData != null)
                        {
                            _logger.LogDebug(
                                LogMessages.Consumer.ConsumerDataflowQueueing,
                                ConsumerSettings.ConsumerName,
                                receivedData.DeliveryTag);

                            await dataflowEngine
                                .EnqueueWorkAsync(receivedData)
                                .ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    LogMessages.Consumer.ConsumerDataflowActionCancelled,
                    ConsumerSettings.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerDataflowError,
                    ConsumerSettings.ConsumerName,
                    ex.Message);
            }
            finally { dataFlowExecLock.Release(); }
        }

        private readonly SemaphoreSlim pipeExecLock = new SemaphoreSlim(1, 1);

        public async Task PipelineStreamEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
        {
            await pipeExecLock
                .WaitAsync(2000)
                .ConfigureAwait(false);

            try
            {
                await foreach (var receivedData in DataBuffer.Reader.ReadAllAsync(token))
                {
                    if (receivedData == null) { continue; }

                    _logger.LogDebug(
                        LogMessages.Consumer.ConsumerPipelineQueueing,
                        ConsumerSettings.ConsumerName,
                        receivedData.DeliveryTag);

                    await pipeline
                        .QueueForExecutionAsync(receivedData)
                        .ConfigureAwait(false);

                    if (waitForCompletion)
                    {
                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerPipelineWaiting,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag);

                        await receivedData
                            .Completion()
                            .ConfigureAwait(false);

                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerPipelineWaitingDone,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag);
                    }

                    if (token.IsCancellationRequested)
                    { return; }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    LogMessages.Consumer.ConsumerPipelineActionCancelled,
                    ConsumerSettings.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerPipelineError,
                    ConsumerSettings.ConsumerName,
                    ex.Message);
            }
            finally { pipeExecLock.Release(); }
        }

        public async Task PipelineExecutionEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
        {
            await pipeExecLock
                .WaitAsync(2000)
                .ConfigureAwait(false);

            try
            {
                while (await DataBuffer.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (DataBuffer.Reader.TryRead(out var receivedData))
                    {
                        if (receivedData == null) { continue; }

                        _logger.LogDebug(
                            LogMessages.Consumer.ConsumerPipelineQueueing,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag);

                        await pipeline
                            .QueueForExecutionAsync(receivedData)
                            .ConfigureAwait(false);

                        if (waitForCompletion)
                        {
                            _logger.LogTrace(
                                LogMessages.Consumer.ConsumerPipelineWaiting,
                                ConsumerSettings.ConsumerName,
                                receivedData.DeliveryTag);

                            await receivedData
                                .Completion()
                                .ConfigureAwait(false);

                            _logger.LogTrace(
                                LogMessages.Consumer.ConsumerPipelineWaitingDone,
                                ConsumerSettings.ConsumerName,
                                receivedData.DeliveryTag);
                        }

                        if (token.IsCancellationRequested)
                        { return; }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    LogMessages.Consumer.ConsumerPipelineActionCancelled,
                    ConsumerSettings.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerPipelineError,
                    ConsumerSettings.ConsumerName,
                    ex.Message);
            }
            finally { pipeExecLock.Release(); }
        }
    }
}
