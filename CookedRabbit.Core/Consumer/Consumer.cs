using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using CookedRabbit.Core.WorkEngines;
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
        public Config Config { get; }

        public IChannelPool ChannelPool { get; }
        public Channel<ReceivedData> DataBuffer { get; set; }

        private IChannelHost ConsumingChannelHost { get; set; }

        public ConsumerOptions ConsumerSettings { get; set; }

        public bool Consuming { get; private set; }
        public bool Shutdown { get; private set; }

        private bool AutoAck { get; set; }
        private bool UseTransientChannel { get; set; }
        private readonly SemaphoreSlim conLock = new SemaphoreSlim(1, 1);

        private byte[] HashKey { get; }

        public Consumer(Config config, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(config, nameof(config));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = config;
            ChannelPool = new ChannelPool(Config);
            HashKey = hashKey;

            ConsumerSettings = Config.GetConsumerSettings(consumerName);
        }

        public Consumer(IChannelPool channelPool, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

            ConsumerSettings = Config.GetConsumerSettings(consumerName);
        }

        public Consumer(IChannelPool channelPool, ConsumerOptions consumerSettings, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerSettings, nameof(consumerSettings));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;
            ConsumerSettings = consumerSettings;
        }

        public async Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true)
        {
            ConsumerSettings = Config.GetConsumerSettings(ConsumerSettings.ConsumerName);

            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

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
                    do { success = await StartConsumingAsync().ConfigureAwait(false); }
                    while (!success);

                    Consuming = true;
                    Shutdown = false;
                }
            }
            finally { conLock.Release(); }
        }

        public async Task StopConsumerAsync(bool immediate = false)
        {
            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

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
            finally { conLock.Release(); }
        }

        private async Task<bool> StartConsumingAsync()
        {
            if (Shutdown)
            { return false; }

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

            return true;
        }

        private async Task SetChannelHostAsync()
        {
            try
            {
                if (UseTransientChannel)
                {
                    ConsumingChannelHost = await ChannelPool
                        .GetTransientChannelAsync(!AutoAck)
                        .ConfigureAwait(false);
                }
                else if (AutoAck)
                {
                    ConsumingChannelHost = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);
                }
                else
                {
                    ConsumingChannelHost = await ChannelPool
                        .GetAckChannelAsync()
                        .ConfigureAwait(false);
                }
            }
            catch { ConsumingChannelHost = null; }
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
                do { success = await StartConsumingAsync().ConfigureAwait(false); }
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
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
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
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
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
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
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
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
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
                        await workAsync(receivedData)
                            .ConfigureAwait(false);

                        receivedData.AckMessage();
                    }
                    catch
                    { receivedData.NackMessage(true); }
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
                        if (await workAsync(receivedData).ConfigureAwait(false))
                        { receivedData.AckMessage(); }
                        else
                        { receivedData.NackMessage(true); }
                    }
                    catch
                    { receivedData.NackMessage(true); }
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
                        if (await workAsync(receivedData, options).ConfigureAwait(false))
                        { receivedData.AckMessage(); }
                        else
                        { receivedData.NackMessage(true); }
                    }
                    catch
                    { receivedData.NackMessage(true); }
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
                                        if (await workAsync(receivedData).ConfigureAwait(false))
                                        { receivedData.AckMessage(); }
                                        else
                                        { receivedData.NackMessage(true); }
                                    }
                                    catch
                                    { receivedData.NackMessage(true); }
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
                                    await pipeline
                                        .QueueForExecutionAsync(receivedData)
                                        .ConfigureAwait(false);

                                    if (waitForCompletion)
                                    {
                                        await receivedData
                                            .Completion()
                                            .ConfigureAwait(false);
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
                    catch { }
                }
            }
            catch { }
            finally { pipeExecLock.Release(); }
        }
    }
}
