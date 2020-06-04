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
    public class MessageConsumer : IConsumer<ReceivedMessage>
    {
        public Config Config { get; }

        public IChannelPool ChannelPool { get; }
        public Channel<ReceivedMessage> DataBuffer { get; set; }

        private IChannelHost ConsumingChannelHost { get; set; }

        public ConsumerOptions ConsumerSettings { get; set; }

        public bool Consuming { get; private set; }
        public bool Shutdown { get; private set; }

        private bool AutoAck { get; set; }
        private bool UseTransientChannel { get; set; }
        private readonly SemaphoreSlim conLock = new SemaphoreSlim(1, 1);

        public MessageConsumer(Config config, string consumerName)
        {
            Guard.AgainstNull(config, nameof(config));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = config;
            ChannelPool = new ChannelPool(Config);
            ConsumerSettings = Config.GetMessageConsumerSettings(consumerName);
        }

        public MessageConsumer(IChannelPool channelPool, string consumerName)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = channelPool.Config;
            ChannelPool = channelPool;

            ConsumerSettings = Config.GetMessageConsumerSettings(consumerName);
        }

        public MessageConsumer(IChannelPool channelPool, ConsumerOptions consumerSettings)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerSettings, nameof(consumerSettings));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            ConsumerSettings = consumerSettings;
        }

        public async Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true)
        {
            ConsumerSettings = Config.GetMessageConsumerSettings(ConsumerSettings.ConsumerName);

            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (ConsumerSettings.Enabled && !Consuming)
                {
                    AutoAck = autoAck;
                    UseTransientChannel = useTransientChannel;

                    DataBuffer = Channel.CreateBounded<ReceivedMessage>(
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
            var rabbitMessage = new ReceivedMessage(ConsumingChannelHost.Channel, bdea, !AutoAck);

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
            var rabbitMessage = new ReceivedMessage(ConsumingChannelHost.Channel, bdea, !AutoAck);

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

        public ChannelReader<ReceivedMessage> GetConsumerBuffer() => DataBuffer.Reader;

        public async ValueTask<ReceivedMessage> ReadAsync()
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

        public async Task<IEnumerable<ReceivedMessage>> ReadUntilEmptyAsync()
        {
            if (!await DataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            var list = new List<ReceivedMessage>();
            await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (DataBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                list.Add(message);
            }

            return list;
        }

        public async IAsyncEnumerable<ReceivedMessage> StreamOutUntilEmptyAsync()
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

        public async IAsyncEnumerable<ReceivedMessage> StreamOutUntilClosedAsync()
        {
            if (!await DataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await foreach (var message in DataBuffer.Reader.ReadAllAsync())
            {
                yield return message;
            }
        }

        public async Task ExecutionEngineAsync(Func<ReceivedMessage, Task> workAsync)
        {
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedMessage in DataBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        await workAsync(receivedMessage)
                            .ConfigureAwait(false);

                        receivedMessage.AckMessage();
                    }
                    catch
                    { receivedMessage.NackMessage(true); }
                }
            }
        }

        public async Task ExecutionEngineAsync(Func<ReceivedMessage, Task<bool>> workAsync)
        {
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedMessage in DataBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        if (await workAsync(receivedMessage).ConfigureAwait(false))
                        { receivedMessage.AckMessage(); }
                        else
                        { receivedMessage.NackMessage(true); }
                    }
                    catch
                    { receivedMessage.NackMessage(true); }
                }
            }
        }

        public async Task ExecutionEngineAsync<TOptions>(Func<ReceivedMessage, TOptions, Task<bool>> workAsync, TOptions options)
        {
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedMessage in DataBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        if (await workAsync(receivedMessage, options).ConfigureAwait(false))
                        { receivedMessage.AckMessage(); }
                        else
                        { receivedMessage.NackMessage(true); }
                    }
                    catch
                    { receivedMessage.NackMessage(true); }
                }
            }
        }

        private readonly SemaphoreSlim parallelExecLock = new SemaphoreSlim(1, 1);

        public async Task ParallelExecutionEngineAsync(Func<ReceivedMessage, Task<bool>> workAsync, int maxDoP = 4, CancellationToken token = default)
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
                        while (DataBuffer.Reader.TryRead(out var receivedMessage))
                        {
                            if (token.IsCancellationRequested)
                            { return; }

                            if (receivedMessage != null)
                            {
                                await parallelLock.WaitAsync().ConfigureAwait(false);

                                _ = Task.Run(ExecuteWorkAsync);

                                async ValueTask ExecuteWorkAsync()
                                {
                                    try
                                    {
                                        if (await workAsync(receivedMessage).ConfigureAwait(false))
                                        { receivedMessage.AckMessage(); }
                                        else
                                        { receivedMessage.NackMessage(true); }
                                    }
                                    catch
                                    { receivedMessage.NackMessage(true); }
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

        public async Task DataflowExecutionEngineAsync(Func<ReceivedMessage, Task<bool>> workBodyAsync, int maxDoP = 4, CancellationToken token = default)
        {
            await dataFlowExecLock.WaitAsync(2000).ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested)
                { return; }

                var messageEngine = new MessageDataflowEngine(workBodyAsync, maxDoP);

                while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    { return; }

                    try
                    {
                        while (DataBuffer.Reader.TryRead(out var receivedMessage))
                        {
                            if (receivedMessage != null)
                            {
                                await messageEngine
                                    .EnqueueWorkAsync(receivedMessage)
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

        public async Task PipelineExecutionEngineAsync<TOut>(IPipeline<ReceivedMessage, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
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
                        while (DataBuffer.Reader.TryRead(out var receivedMessage))
                        {
                            if (token.IsCancellationRequested)
                            { return; }

                            if (receivedMessage != null)
                            {
                                await workLock.WaitAsync().ConfigureAwait(false);

                                try
                                {
                                    await pipeline
                                        .QueueForExecutionAsync(receivedMessage)
                                        .ConfigureAwait(false);

                                    if (waitForCompletion)
                                    {
                                        await receivedMessage
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
