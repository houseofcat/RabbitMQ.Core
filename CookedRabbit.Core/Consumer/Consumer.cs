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
        ConsumerOptions Options { get; }
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

    public class Consumer : IConsumer<ReceivedData>, IDisposable
    {
        private readonly ILogger<Consumer> _logger;
        private readonly SemaphoreSlim _conLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _pipeExecLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _dataFlowExecLock = new SemaphoreSlim(1, 1);
        private bool _disposedValue;

        public Config Config { get; }

        public IChannelPool ChannelPool { get; }
        public Channel<ReceivedData> DataBuffer { get; private set; }

        private IChannelHost ConsumingChannelHost { get; set; }

        public ConsumerOptions Options { get; }

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

            Options = Config.GetConsumerSettings(consumerName);
        }

        public Consumer(IChannelPool channelPool, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            _logger = LogHelper.GetLogger<Consumer>();
            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

            Options = Config.GetConsumerSettings(consumerName);
        }

        public Consumer(IChannelPool channelPool, ConsumerOptions consumerSettings, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerSettings, nameof(consumerSettings));

            _logger = LogHelper.GetLogger<Consumer>();
            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;
            Options = consumerSettings;
        }

        public async Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true)
        {
            await _conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (!Started && Options.Enabled)
                {
                    Shutdown = false;
                    DataBuffer = Channel.CreateBounded<ReceivedData>(
                    new BoundedChannelOptions(Options.BatchSize.Value)
                    {
                        FullMode = Options.BehaviorWhenFull.Value
                    });

                    await DataBuffer
                        .Writer
                        .WaitToWriteAsync()
                        .ConfigureAwait(false);

                    bool success;
                    do
                    {
                        _logger.LogTrace(LogMessages.Consumer.StartingConsumerLoop, Options.ConsumerName);
                        success = await StartConsumingAsync().ConfigureAwait(false);
                    }
                    while (!success);

                    _logger.LogDebug(LogMessages.Consumer.Started, Options.ConsumerName);

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

            _logger.LogDebug(LogMessages.Consumer.StopConsumer, Options.ConsumerName);

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
                        Options.ConsumerName);
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
                Options.ConsumerName);

            await SetChannelHostAsync()
                .ConfigureAwait(false);

            if (ConsumingChannelHost == null) { return false; }

            if (Config.FactorySettings.EnableDispatchConsumersAsync)
            {
                var consumer = CreateAsyncConsumer();
                if (consumer == null) { return false; }

                ConsumingChannelHost
                    .GetChannel()
                    .BasicConsume(
                        Options.QueueName,
                        Options.AutoAck ?? false,
                        Options.ConsumerName,
                        Options.NoLocal ?? false,
                        Options.Exclusive ?? false,
                        null,
                        consumer);
            }
            else
            {
                var consumer = CreateConsumer();
                if (consumer == null) { return false; }

                ConsumingChannelHost
                    .GetChannel()
                    .BasicConsume(
                        Options.QueueName,
                        Options.AutoAck ?? false,
                        Options.ConsumerName,
                        Options.NoLocal ?? false,
                        Options.Exclusive ?? false,
                        null,
                        consumer);
            }

            _logger.LogInformation(
                LogMessages.Consumer.StartedConsumer,
                Options.ConsumerName);

            return true;
        }

        private async Task SetChannelHostAsync()
        {
            try
            {
                if (Options.UseTransientChannels ?? true)
                {
                    var autoAck = Options.AutoAck ?? false;
                    _logger.LogTrace(LogMessages.Consumer.GettingTransientChannelHost, Options.ConsumerName);
                    ConsumingChannelHost = await ChannelPool
                        .GetTransientChannelAsync(!autoAck)
                        .ConfigureAwait(false);
                }
                else if (Options.AutoAck ?? false)
                {
                    _logger.LogTrace(LogMessages.Consumer.GettingChannelHost, Options.ConsumerName);
                    ConsumingChannelHost = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);
                }
                else
                {
                    _logger.LogTrace(LogMessages.Consumer.GettingAckChannelHost, Options.ConsumerName);
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
                    Options.ConsumerName,
                    ConsumingChannelHost?.ChannelId ?? 0ul);
            }
            else
            {
                _logger.LogError(
                    LogMessages.Consumer.ChannelNotEstablished,
                    Options.ConsumerName);
            }
        }

        private EventingBasicConsumer CreateConsumer()
        {
            EventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.GetChannel().BasicQos(0, Options.BatchSize.Value, false);
                consumer = new EventingBasicConsumer(ConsumingChannelHost.GetChannel());

                consumer.Received += ReceiveHandler;
                consumer.Shutdown += ConsumerShutdown;
            }
            catch { }

            return consumer;
        }

        private async void ReceiveHandler(object _, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(ConsumingChannelHost.GetChannel(), bdea, !(Options.AutoAck ?? false), HashKey);

            _logger.LogDebug(
                LogMessages.Consumer.ConsumerMessageReceived,
                Options.ConsumerName,
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
            await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false);
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            AsyncEventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.GetChannel().BasicQos(0, Options.BatchSize.Value, false);
                consumer = new AsyncEventingBasicConsumer(ConsumingChannelHost.GetChannel());

                consumer.Received += ReceiveHandlerAsync;
                consumer.Shutdown += ConsumerShutdownAsync;
            }
            catch { }

            return consumer;
        }

        private async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(ConsumingChannelHost.GetChannel(), bdea, !(Options.AutoAck ?? false), HashKey);

            _logger.LogDebug(
                LogMessages.Consumer.ConsumerAsyncMessageReceived,
                Options.ConsumerName,
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
            await HandleUnexpectedShutdownAsync(e).ConfigureAwait(false);
        }

        private async Task HandleUnexpectedShutdownAsync(ShutdownEventArgs e)
        {
            if (!Shutdown)
            {
                bool success;
                do
                {
                    _logger.LogWarning(
                        LogMessages.Consumer.ConsumerShutdownEvent,
                        Options.ConsumerName,
                        e.ReplyText);

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
        public async Task DataflowExecutionEngineAsync(Func<ReceivedData, Task<bool>> workBodyAsync, int maxDoP = 4, CancellationToken token = default)
        {
            await _dataFlowExecLock.WaitAsync(2000).ConfigureAwait(false);

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
                                Options.ConsumerName,
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
                    Options.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerDataflowError,
                    Options.ConsumerName,
                    ex.Message);
            }
            finally { _dataFlowExecLock.Release(); }
        }

        public async Task PipelineStreamEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
        {
            await _pipeExecLock
                .WaitAsync(2000)
                .ConfigureAwait(false);

            try
            {
                await foreach (var receivedData in DataBuffer.Reader.ReadAllAsync(token))
                {
                    if (receivedData == null) { continue; }

                    _logger.LogDebug(
                        LogMessages.Consumer.ConsumerPipelineQueueing,
                        Options.ConsumerName,
                        receivedData.DeliveryTag);

                    await pipeline
                        .QueueForExecutionAsync(receivedData)
                        .ConfigureAwait(false);

                    if (waitForCompletion)
                    {
                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerPipelineWaiting,
                            Options.ConsumerName,
                            receivedData.DeliveryTag);

                        await receivedData
                            .Completion()
                            .ConfigureAwait(false);

                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerPipelineWaitingDone,
                            Options.ConsumerName,
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
                    Options.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerPipelineError,
                    Options.ConsumerName,
                    ex.Message);
            }
            finally { _pipeExecLock.Release(); }
        }

        public async Task PipelineExecutionEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
        {
            await _pipeExecLock
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
                            Options.ConsumerName,
                            receivedData.DeliveryTag);

                        await pipeline
                            .QueueForExecutionAsync(receivedData)
                            .ConfigureAwait(false);

                        if (waitForCompletion)
                        {
                            _logger.LogTrace(
                                LogMessages.Consumer.ConsumerPipelineWaiting,
                                Options.ConsumerName,
                                receivedData.DeliveryTag);

                            await receivedData
                                .Completion()
                                .ConfigureAwait(false);

                            _logger.LogTrace(
                                LogMessages.Consumer.ConsumerPipelineWaitingDone,
                                Options.ConsumerName,
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
                    Options.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.Consumer.ConsumerPipelineError,
                    Options.ConsumerName,
                    ex.Message);
            }
            finally { _pipeExecLock.Release(); }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _dataFlowExecLock.Dispose();
                    _pipeExecLock.Dispose();
                    _conLock.Dispose();
                }

                DataBuffer = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
