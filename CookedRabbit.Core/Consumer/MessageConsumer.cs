using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using CookedRabbit.Core.WorkEngines;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utf8Json;

namespace CookedRabbit.Core
{
    public class MessageConsumer
    {
        private Config Config { get; }

        public ChannelPool ChannelPool { get; }
        private Channel<ReceivedMessage> MessageBuffer { get; set; }

        private ChannelHost ConsumingChannelHost { get; set; }

        public string ConsumerName { get; }
        public string QueueName { get; }
        public bool NoLocal { get; }
        public bool Exclusive { get; }
        public ushort QosPrefetchCount { get; }

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

            var conSettings = Config.GetConsumerSettings(consumerName);
            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        public MessageConsumer(ChannelPool channelPool, string consumerName)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = channelPool.Config;
            ChannelPool = channelPool;

            var conSettings = Config.GetConsumerSettings(consumerName);
            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        public MessageConsumer(ChannelPool channelPool, ConsumerOptions consumerSettings, string consumerName)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = channelPool.Config;
            ChannelPool = channelPool;

            ConsumerName = consumerSettings.ConsumerName;
            QueueName = consumerSettings.QueueName;
            NoLocal = consumerSettings.NoLocal;
            Exclusive = consumerSettings.Exclusive;
            QosPrefetchCount = consumerSettings.QosPrefetchCount;
        }

        public async Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true)
        {
            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (!Consuming)
                {
                    AutoAck = autoAck;
                    UseTransientChannel = useTransientChannel;

                    var conSettings = Config.GetConsumerSettings(ConsumerName);

                    MessageBuffer = Channel.CreateBounded<ReceivedMessage>(
                    new BoundedChannelOptions(conSettings.MessageBufferSize)
                    {
                        FullMode = conSettings.BehaviorWhenFull
                    });

                    await MessageBuffer
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
                MessageBuffer.Writer.Complete();

                if (immediate)
                {
                    ConsumingChannelHost.Close();
                }
                else
                {
                    await MessageBuffer
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
                    .BasicConsume(QueueName, AutoAck, ConsumerName, NoLocal, Exclusive, null, consumer);
            }
            else
            {
                var consumer = CreateConsumer();
                if (consumer == null) { return false; }

                ConsumingChannelHost
                    .Channel
                    .BasicConsume(QueueName, AutoAck, ConsumerName, NoLocal, Exclusive, null, consumer);
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
                ConsumingChannelHost.Channel.BasicQos(0, QosPrefetchCount, false);
                consumer = new EventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandler;
                consumer.Shutdown += ConsumerShutdown;
            }
            catch { }

            return consumer;
        }

#pragma warning disable RCS1163 // Unused parameter.
        private async void ReceiveHandler(object o, BasicDeliverEventArgs bdea)
#pragma warning restore RCS1163 // Unused parameter.
        {
            var rabbitMessage = new ReceivedMessage(ConsumingChannelHost.Channel, bdea, !AutoAck);

            if (await MessageBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false))
            {
                await MessageBuffer
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
                ConsumingChannelHost.Channel.BasicQos(0, QosPrefetchCount, false);
                consumer = new AsyncEventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandlerAsync;
                consumer.Shutdown += ConsumerShutdownAsync;
            }
            catch { }

            return consumer;
        }

#pragma warning disable RCS1163 // Unused parameter.
        private async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
#pragma warning restore RCS1163 // Unused parameter.
        {
            var rabbitMessage = new ReceivedMessage(ConsumingChannelHost.Channel, bdea, !AutoAck);

            if (await MessageBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                await MessageBuffer
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

        /// <summary>
        /// Simple retrieve message (byte[]) from queue. Null if nothing was available or on error. Exception possible when retrieving Channel.
        /// <para>AutoAcks message.</para>
        /// </summary>
        /// <param name="queueName"></param>
        public async Task<byte[]> GetAsync(string queueName)
        {
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            BasicGetResult result = null;
            var error = false;
            try
            {
                result = chanHost
                    .Channel
                    .BasicGet(queueName, true);
            }
            catch { error = true; }

            await ChannelPool
                .ReturnChannelAsync(chanHost, error)
                .ConfigureAwait(false);

            return result?.Body;
        }

        /// <summary>
        /// Simple retrieve message (byte[]) from queue and convert to <see cref="{T}" /> efficiently. Default (assumed null) if nothing was available (or on transmission error). Exception possible when retrieving Channel.
        /// <para>AutoAcks message.</para>
        /// </summary>
        /// <param name="queueName"></param>
        public async Task<T> GetAsync<T>(string queueName)
        {
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            BasicGetResult result = null;
            var error = false;
            try
            {
                result = chanHost
                    .Channel
                    .BasicGet(queueName, true);
            }
            catch { error = true; }

            await ChannelPool
                .ReturnChannelAsync(chanHost, error)
                .ConfigureAwait(false);

            return result != null ? JsonSerializer.Deserialize<T>(result.Body) : default;
        }

        public ChannelReader<ReceivedMessage> GetConsumerMessageBuffer() => MessageBuffer.Reader;

        public async ValueTask<ReceivedMessage> ReadMessageAsync()
        {
            if (!await MessageBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            return await MessageBuffer
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReceivedMessage>> ReadMessagesUntilEmptyAsync()
        {
            if (!await MessageBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            var list = new List<ReceivedMessage>();
            await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (MessageBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                list.Add(message);
            }

            return list;
        }

#if CORE2
        public async Task ExecutionEngineAsync(Func<ReceivedMessage, Task> workAsync)
        {
            while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                try
                {
                    while (MessageBuffer.Reader.TryRead(out var receivedMessage))
                    {
                        if (receivedMessage != null)
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
                catch { }
            }
        }

        public async Task ExecutionEngineAsync(Func<ReceivedMessage, Task<bool>> workAsync)
        {
            while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                try
                {
                    while (MessageBuffer.Reader.TryRead(out var receivedMessage))
                    {
                        if (receivedMessage != null)
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
                catch { }
            }
        }

        public async Task ExecutionEngineAsync<TOptions>(Func<ReceivedMessage, TOptions, Task<bool>> workAsync, TOptions options)
        {
            while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                try
                {
                    while (MessageBuffer.Reader.TryRead(out var receivedMessage))
                    {
                        if (receivedMessage != null)
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
                catch { }
            }
        }
#endif

#if CORE3
        public async IAsyncEnumerable<ReceivedMessage> StreamOutMessagesUntilEmptyAsync()
        {
            if (!await MessageBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (MessageBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                yield return message;
            }
        }

        public async IAsyncEnumerable<ReceivedMessage> StreamOutMessagesUntilClosedAsync()
        {
            if (!await MessageBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await foreach (var message in MessageBuffer.Reader.ReadAllAsync())
            {
                yield return message;
            }
        }

        public async Task ExecutionEngineAsync(Func<ReceivedMessage, Task> workAsync)
        {
            while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedMessage in MessageBuffer.Reader.ReadAllAsync())
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
            while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedMessage in MessageBuffer.Reader.ReadAllAsync())
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
            while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedMessage in MessageBuffer.Reader.ReadAllAsync())
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
#endif

        public async Task ParallelExecutionEngineAsync(Func<ReceivedMessage, Task<bool>> workAsync, int maxDoP = 4)
        {
            var parallelLock = new SemaphoreSlim(1, maxDoP);
            while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                try
                {
                    while (MessageBuffer.Reader.TryRead(out var receivedMessage))
                    {
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
                }
                catch { }
            }
        }

        private readonly SemaphoreSlim dataFlowExecLock = new SemaphoreSlim(1,1);

        public async Task DataflowExecutionEngineAsync(Func<ReceivedMessage, Task<bool>> workBodyAsync, int maxDoP = 4)
        {
            await dataFlowExecLock.WaitAsync(2000).ConfigureAwait(false);

            var messageEngine = new MessageDataflowEngine(workBodyAsync, maxDoP);

            try
            {
                while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    try
                    {
                        while (MessageBuffer.Reader.TryRead(out var receivedMessage))
                        {
                            if (receivedMessage != null)
                            {
                                await messageEngine
                                    .EnqueueWorkAsync(receivedMessage)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    catch { }
                }
            }
            finally { dataFlowExecLock.Release(); }
        }

        private readonly SemaphoreSlim pipeExecLock = new SemaphoreSlim(1, 1);

        public async Task WorkflowExecutionEngineAsync<TOut>(Pipeline<ReceivedMessage, TOut> pipeline)
        {
            await pipeExecLock
                .WaitAsync(2000)
                .ConfigureAwait(false);

            try
            {
                while (await MessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    try
                    {
                        while (MessageBuffer.Reader.TryRead(out var receivedMessage))
                        {
                            if (receivedMessage != null)
                            {
                                await pipeline
                                    .QueueForExecutionAsync(receivedMessage)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    catch { }
                }
            }
            finally { pipeExecLock.Release(); }
        }
    }
}
