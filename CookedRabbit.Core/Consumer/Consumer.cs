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
        ConsumerOptions ConsumerSettings { get; set; }
        bool Consuming { get; }
        bool Shutdown { get; }

        Task DataflowExecutionEngineAsync(Func<TFromQueue, Task<bool>> workBodyAsync, int maxDoP = 4, CancellationToken token = default);
        Task ExecutionEngineAsync(Func<TFromQueue, Task<bool>> workAsync);
        Task ExecutionEngineAsync(Func<TFromQueue, Task> workAsync);
        Task ExecutionEngineAsync<TOptions>(Func<TFromQueue, TOptions, Task<bool>> workAsync, TOptions options);

        ChannelReader<TFromQueue> GetConsumerBuffer();
        Task ParallelExecutionEngineAsync(Func<TFromQueue, Task<bool>> workAsync, int maxDoP = 4, CancellationToken token = default);
        Task PipelineExecutionEngineAsync<TLocalOut>(IPipeline<TFromQueue, TLocalOut> pipeline, bool waitForCompletion, CancellationToken token = default);
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

        public ConsumerOptions ConsumerSettings { get; set; }

        public bool Consuming { get; private set; }
        public bool Shutdown { get; private set; }

        private bool AutoAck { get; set; }
        private bool UseTransientChannel { get; set; }


        private byte[] HashKey { get; }

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
            ConsumerSettings = Config.GetConsumerSettings(ConsumerSettings.ConsumerName);

            await _conLock
                .WaitAsync()
                .ConfigureAwait(false);

            _logger.LogTrace(LogMessages.Consumer.Started, ConsumerSettings.ConsumerName);

            try
            {
                if (ConsumerSettings.Enabled && !Consuming)
                {
                    AutoAck = autoAck;
                    UseTransientChannel = useTransientChannel;

                    DataBuffer = Channel.CreateBounded<ReceivedData>(
                    new BoundedChannelOptions(ConsumerSettings.BufferSize)
                    {
                        FullMode = ConsumerSettings.BehaviorWhenFull
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

                    Consuming = true;
                    Shutdown = false;
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
                DataBuffer.Writer.Complete();

                if (immediate)
                {
                    ConsumingChannelHost.Close();
                }
                else
                {
                    await DataBuffer
                        .Reader
                        .Completion
                        .ConfigureAwait(false);
                }

                Shutdown = true;
            }
            finally { _conLock.Release(); }

            _logger.LogDebug(
                LogMessages.Consumer.StoppedConsumer,
                ConsumerSettings.ConsumerName);
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
                        AutoAck,
                        ConsumerSettings.ConsumerName,
                        ConsumerSettings.NoLocal,
                        ConsumerSettings.Exclusive,
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
                        AutoAck,
                        ConsumerSettings.ConsumerName,
                        ConsumerSettings.NoLocal,
                        ConsumerSettings.Exclusive,
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
                if (UseTransientChannel)
                {
                    _logger.LogTrace(LogMessages.Consumer.GettingTransientChannelHost, ConsumerSettings.ConsumerName);
                    ConsumingChannelHost = await ChannelPool
                        .GetTransientChannelAsync(!AutoAck)
                        .ConfigureAwait(false);
                }
                else if (AutoAck)
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

            _logger.LogDebug(
                LogMessages.Consumer.ChannelEstablished,
                ConsumerSettings.ConsumerName,
                ConsumingChannelHost.ChannelId);
        }

        private EventingBasicConsumer CreateConsumer()
        {
            EventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.Channel.BasicQos(0, ConsumerSettings.QosPrefetchCount, false);
                consumer = new EventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandler;
                consumer.Shutdown += ConsumerShutdown;
            }
            catch { }

            return consumer;
        }

        private async void ReceiveHandler(object o, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(ConsumingChannelHost.Channel, bdea, !AutoAck, HashKey);

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
            if (!Shutdown)
            {
                bool success;
                do { success = await StartConsumingAsync().ConfigureAwait(false); }
                while (!success);
            }
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            AsyncEventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.Channel.BasicQos(0, ConsumerSettings.QosPrefetchCount, false);
                consumer = new AsyncEventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandlerAsync;
                consumer.Shutdown += ConsumerShutdownAsync;
            }
            catch { }

            return consumer;
        }

        private async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
        {
            var rabbitMessage = new ReceivedData(ConsumingChannelHost.Channel, bdea, !AutoAck, HashKey);

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
                while (!success);
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

        public async Task ExecutionEngineAsync(Func<ReceivedData, Task> workAsync)
        {
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedData in DataBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerExecution,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag);

                        await workAsync(receivedData)
                            .ConfigureAwait(false);

                        _logger.LogDebug(
                            LogMessages.Consumer.ConsumerExecutionSuccess,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag);

                        receivedData.AckMessage();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            LogMessages.Consumer.ConsumerExecutionError,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag,
                            ex.Message);

                        receivedData.NackMessage(true);
                    }
                }
            }
        }

        public async Task ExecutionEngineAsync(Func<ReceivedData, Task<bool>> workAsync)
        {
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedData in DataBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerExecution,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag);

                        if (await workAsync(receivedData).ConfigureAwait(false))
                        {
                            _logger.LogDebug(
                                LogMessages.Consumer.ConsumerExecutionSuccess,
                                ConsumerSettings.ConsumerName,
                                receivedData.DeliveryTag);

                            receivedData.AckMessage();
                        }
                        else
                        {
                            _logger.LogWarning(
                                LogMessages.Consumer.ConsumerExecutionFailure,
                                ConsumerSettings.ConsumerName,
                                receivedData.DeliveryTag);

                            receivedData.NackMessage(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            LogMessages.Consumer.ConsumerExecutionError,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag,
                            ex.Message);

                        receivedData.NackMessage(true);
                    }
                }
            }
        }

        public async Task ExecutionEngineAsync<TOptions>(Func<ReceivedData, TOptions, Task<bool>> workAsync, TOptions options)
        {
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedData in DataBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        _logger.LogTrace(
                            LogMessages.Consumer.ConsumerExecution,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag);

                        if (await workAsync(receivedData, options).ConfigureAwait(false))
                        {
                            _logger.LogDebug(
                                LogMessages.Consumer.ConsumerExecutionSuccess,
                                ConsumerSettings.ConsumerName,
                                receivedData.DeliveryTag);

                            receivedData.AckMessage();
                        }
                        else
                        {
                            _logger.LogWarning(
                                LogMessages.Consumer.ConsumerExecutionFailure,
                                ConsumerSettings.ConsumerName,
                                receivedData.DeliveryTag);

                            receivedData.NackMessage(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            LogMessages.Consumer.ConsumerExecutionError,
                            ConsumerSettings.ConsumerName,
                            receivedData.DeliveryTag,
                            ex.Message);

                        receivedData.NackMessage(true);
                    }
                }
            }
        }

        private readonly SemaphoreSlim parallelExecLock = new SemaphoreSlim(1, 1);

        public async Task ParallelExecutionEngineAsync(Func<ReceivedData, Task<bool>> workAsync, int maxDoP = 4, CancellationToken token = default)
        {
            await parallelExecLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested)
                { return; }

                while (await DataBuffer.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    { return; }

                    var parallelLock = new SemaphoreSlim(1, maxDoP);

                    try
                    {
                        while (DataBuffer.Reader.TryRead(out var receivedData))
                        {
                            if (token.IsCancellationRequested)
                            { return; }

                            if (receivedData != null)
                            {
                                await parallelLock.WaitAsync().ConfigureAwait(false);

                                _ = Task.Run(ExecuteWorkAsync);

                                async ValueTask ExecuteWorkAsync()
                                {
                                    try
                                    {
                                        _logger.LogDebug(
                                            LogMessages.Consumer.ConsumerParallelExecution,
                                            ConsumerSettings.ConsumerName,
                                            receivedData.DeliveryTag);

                                        if (await workAsync(receivedData).ConfigureAwait(false))
                                        {
                                            _logger.LogDebug(
                                                LogMessages.Consumer.ConsumerParallelExecutionSuccess,
                                                ConsumerSettings.ConsumerName,
                                                receivedData.DeliveryTag);

                                            receivedData.AckMessage();
                                        }
                                        else
                                        {
                                            _logger.LogWarning(
                                                LogMessages.Consumer.ConsumerParallelExecutionFailure,
                                                ConsumerSettings.ConsumerName,
                                                receivedData.DeliveryTag);

                                            receivedData.NackMessage(true);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(
                                            LogMessages.Consumer.ConsumerParallelExecutionFailure,
                                            ConsumerSettings.ConsumerName,
                                            receivedData.DeliveryTag,
                                            ex.Message);

                                        receivedData.NackMessage(true);
                                    }
                                    finally
                                    { parallelLock.Release(); }
                                }
                            }
                        }

                        await Task
                            .Delay(ConsumerSettings.SleepOnIdleInterval)
                            .ConfigureAwait(false);
                    }
                    catch { }
                }
            }
            finally { parallelExecLock.Release(); }
        }

        private readonly SemaphoreSlim dataFlowExecLock = new SemaphoreSlim(1, 1);

        public async Task DataflowExecutionEngineAsync(Func<ReceivedData, Task<bool>> workBodyAsync, int maxDoP = 4, CancellationToken token = default)
        {
            await dataFlowExecLock.WaitAsync(2000).ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested)
                { return; }

                var dataflowEngine = new DataflowEngine(workBodyAsync, maxDoP);

                while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    { return; }

                    try
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

                        await Task
                            .Delay(ConsumerSettings.SleepOnIdleInterval)
                            .ConfigureAwait(false);
                    }
                    catch { }
                }
            }
            catch { }
            finally { dataFlowExecLock.Release(); }
        }

        private readonly SemaphoreSlim pipeExecLock = new SemaphoreSlim(1, 1);

        public async Task PipelineExecutionEngineAsync<TOut>(IPipeline<ReceivedData, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
        {
            await pipeExecLock
                .WaitAsync(2000)
                .ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested)
                { return; }

                var workLock = new SemaphoreSlim(1, pipeline.MaxDegreeOfParallelism);

                while (await DataBuffer.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    { return; }

                    try
                    {
                        while (DataBuffer.Reader.TryRead(out var receivedData))
                        {
                            if (token.IsCancellationRequested)
                            { return; }

                            if (receivedData != null)
                            {
                                await workLock.WaitAsync().ConfigureAwait(false);

                                try
                                {
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
                                }
                                finally
                                { workLock.Release(); }
                            }
                        }

                        await Task
                            .Delay(ConsumerSettings.SleepOnIdleInterval)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) 
                    {
                        _logger.LogError(
                            LogMessages.Consumer.ConsumerPipelineError,
                            ConsumerSettings.ConsumerName,
                            ex.Message);
                    }
                }
            }
            finally { pipeExecLock.Release(); }
        }
    }
}
