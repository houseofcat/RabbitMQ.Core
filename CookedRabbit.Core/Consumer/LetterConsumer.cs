using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using CookedRabbit.Core.WorkEngines;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utf8Json;

namespace CookedRabbit.Core
{
    public class LetterConsumer
    {
        public Config Config { get; }

        public ChannelPool ChannelPool { get; }
        private Channel<ReceivedLetter> LetterBuffer { get; set; }

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
        private byte[] HashKey { get; }

        public LetterConsumer(Config config, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(config, nameof(config));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = config;
            ChannelPool = new ChannelPool(Config);
            HashKey = hashKey;

            var conSettings = Config.GetLetterConsumerSettings(consumerName);
            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        public LetterConsumer(ChannelPool channelPool, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

            var conSettings = Config.GetLetterConsumerSettings(consumerName);
            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        public LetterConsumer(ChannelPool channelPool, ConsumerOptions consumerSettings, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerSettings, nameof(consumerSettings));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

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

                    var conSettings = Config.GetLetterConsumerSettings(ConsumerName);

                    LetterBuffer = Channel.CreateBounded<ReceivedLetter>(
                        new BoundedChannelOptions(conSettings.MessageBufferSize)
                        {
                            FullMode = conSettings.BehaviorWhenFull
                        });

                    await LetterBuffer
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
                LetterBuffer.Writer.Complete();

                if (immediate)
                {
                    ConsumingChannelHost.Close();
                }
                else
                {
                    await LetterBuffer
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
            var receivedLetter = new ReceivedLetter(ConsumingChannelHost.Channel, bdea, !AutoAck);

            var decryptFailed = false;
            if (receivedLetter.Letter.LetterMetadata.Encrypted && (HashKey?.Length > 0))
            {
                try
                {
                    receivedLetter.Letter.Body = AesEncrypt.Decrypt(receivedLetter.Letter.Body, HashKey);
                    receivedLetter.Letter.LetterMetadata.Encrypted = false;
                }
                catch { decryptFailed = true; }
            }

            if (!decryptFailed && receivedLetter.Letter.LetterMetadata.Compressed)
            {
                try
                {
                    receivedLetter.Letter.Body = await Gzip.DecompressAsync(receivedLetter.Letter.Body).ConfigureAwait(false);
                    receivedLetter.Letter.LetterMetadata.Compressed = false;
                }
                catch { }
            }

            if (await LetterBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false))
            {
                await LetterBuffer
                    .Writer
                    .WriteAsync(receivedLetter);
            }
        }

        private async void ConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            bool success;
            do { success = await StartConsumingAsync().ConfigureAwait(false); }
            while (!success);
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
            var receivedLetter = new ReceivedLetter(ConsumingChannelHost.Channel, bdea, !AutoAck);

            var decryptFailed = false;
            if (receivedLetter.Letter.LetterMetadata.Encrypted && (HashKey?.Length > 0))
            {
                try
                {
                    receivedLetter.Letter.Body = AesEncrypt.Decrypt(receivedLetter.Letter.Body, HashKey);
                    receivedLetter.Letter.LetterMetadata.Encrypted = false;
                }
                catch { decryptFailed = true; }
            }

            if (!decryptFailed && receivedLetter.Letter.LetterMetadata.Compressed)
            {
                try
                {
                    receivedLetter.Letter.Body = await Gzip.DecompressAsync(receivedLetter.Letter.Body).ConfigureAwait(false);
                    receivedLetter.Letter.LetterMetadata.Compressed = false;
                }
                catch { }
            }

            if (await LetterBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                await LetterBuffer
                    .Writer
                    .WriteAsync(receivedLetter);
            }
        }

        private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
        {
            bool success;
            do { success = await StartConsumingAsync().ConfigureAwait(false); }
            while (!success);
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

        public ChannelReader<ReceivedLetter> GetConsumerLetterBuffer() => LetterBuffer.Reader;

        public async ValueTask<ReceivedLetter> ReadLetterAsync()
        {
            if (!await LetterBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            return await LetterBuffer
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReceivedLetter>> ReadLettersUntilEmptyAsync()
        {
            if (!await LetterBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            var list = new List<ReceivedLetter>();
            await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (LetterBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                list.Add(message);
            }

            return list;
        }

#if CORE2
        public async Task ExecutionEngineAsync(Func<ReceivedLetter, Task> workAsync)
        {
            while (await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                try
                {
                    while (LetterBuffer.Reader.TryRead(out var receivedLetter))
                    {
                        try
                        {
                            await workAsync(receivedLetter)
                                .ConfigureAwait(false);

                            receivedLetter.AckMessage();
                        }
                        catch
                        { receivedLetter.NackMessage(true); }
                    }
                }
                catch { }
            }
        }

        public async Task ExecutionEngineAsync(Func<ReceivedLetter, Task<bool>> workAsync)
        {
            while (await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                try
                {
                    while (LetterBuffer.Reader.TryRead(out var receivedLetter))
                    {
                        try
                        {
                            if (await workAsync(receivedLetter).ConfigureAwait(false))
                            { receivedLetter.AckMessage(); }
                            else
                            { receivedLetter.NackMessage(true); }
                        }
                        catch
                        { receivedLetter.NackMessage(true); }
                    }
                }
                catch { }
            }
        }

        public async Task ExecutionEngineAsync<TOptions>(Func<ReceivedLetter, TOptions, Task<bool>> workAsync, TOptions options)
        {
            while (await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                try
                {
                    while (LetterBuffer.Reader.TryRead(out var receivedLetter))
                    {
                        try
                        {
                            if (await workAsync(receivedLetter, options).ConfigureAwait(false))
                            { receivedLetter.AckMessage(); }
                            else
                            { receivedLetter.NackMessage(true); }
                        }
                        catch
                        { receivedLetter.NackMessage(true); }
                    }
                }
                catch { }
            }
        }
#endif

#if CORE3
        public async IAsyncEnumerable<ReceivedLetter> StreamOutLettersUntilEmptyAsync()
        {
            if (!await LetterBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (LetterBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                yield return message;
            }
        }

        public async IAsyncEnumerable<ReceivedLetter> StreamOutLettersUntilClosedAsync()
        {
            if (!await LetterBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await foreach (var message in LetterBuffer.Reader.ReadAllAsync())
            {
                yield return message;
            }
        }

        public async Task ExecutionEngineAsync(Func<ReceivedLetter, Task> workAsync)
        {
            while (await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedLetter in LetterBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        await workAsync(receivedLetter)
                            .ConfigureAwait(false);

                        receivedLetter.AckMessage();
                    }
                    catch
                    { receivedLetter.NackMessage(true); }
                }
            }
        }

        public async Task ExecutionEngineAsync(Func<ReceivedLetter, Task<bool>> workAsync)
        {
            while (await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedLetter in LetterBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        if (await workAsync(receivedLetter).ConfigureAwait(false))
                        { receivedLetter.AckMessage(); }
                        else
                        { receivedLetter.NackMessage(true); }
                    }
                    catch
                    { receivedLetter.NackMessage(true); }
                }
            }
        }

        public async Task ExecutionEngineAsync<TOptions>(Func<ReceivedLetter, TOptions, Task<bool>> workAsync, TOptions options)
        {
            while (await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedLetter in LetterBuffer.Reader.ReadAllAsync())
                {
                    try
                    {
                        if (await workAsync(receivedLetter, options).ConfigureAwait(false))
                        { receivedLetter.AckMessage(); }
                        else
                        { receivedLetter.NackMessage(true); }
                    }
                    catch
                    { receivedLetter.NackMessage(true); }
                }
            }
        }
#endif

        private readonly SemaphoreSlim parallelExecLock = new SemaphoreSlim(1, 1);

        public async Task ParallelExecutionEngineAsync(Func<ReceivedLetter, Task<bool>> workAsync, int maxDoP = 4, CancellationToken token = default)
        {
            await parallelExecLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested)
                { return; }

                while (await LetterBuffer.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    { return; }

                    var parallelLock = new SemaphoreSlim(1, maxDoP);

                    try
                    {
                        while (LetterBuffer.Reader.TryRead(out var receivedLetter))
                        {
                            if (token.IsCancellationRequested)
                            { return; }

                            if (receivedLetter != null)
                            {
                                await parallelLock.WaitAsync().ConfigureAwait(false);

                                _ = Task.Run(ExecuteWorkAsync);

                                async ValueTask ExecuteWorkAsync()
                                {
                                    try
                                    {
                                        if (await workAsync(receivedLetter).ConfigureAwait(false))
                                        { receivedLetter.AckMessage(); }
                                        else
                                        { receivedLetter.NackMessage(true); }
                                    }
                                    catch
                                    { receivedLetter.NackMessage(true); }
                                    finally
                                    { parallelLock.Release(); }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            finally { parallelExecLock.Release(); }
        }

        private readonly SemaphoreSlim dataFlowExecLock = new SemaphoreSlim(1, 1);

        public async Task DataflowExecutionEngineAsync(Func<ReceivedLetter, Task<bool>> workBodyAsync, int maxDoP = 4, CancellationToken token = default)
        {
            await dataFlowExecLock.WaitAsync(2000).ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested)
                { return; }

                var letterEngine = new LetterDataflowEngine(workBodyAsync, maxDoP);

                while (await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    { return; }

                    try
                    {
                        while (LetterBuffer.Reader.TryRead(out var receivedLetter))
                        {
                            if (receivedLetter != null)
                            {
                                await letterEngine
                                    .EnqueueWorkAsync(receivedLetter)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            finally { dataFlowExecLock.Release(); }
        }

        private readonly SemaphoreSlim pipeExecLock = new SemaphoreSlim(1, 1);

        public async Task PipelineExecutionEngineAsync<TOut>(Pipeline<ReceivedLetter, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
        {
            await pipeExecLock
                .WaitAsync(2000)
                .ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested)
                { return; }

                var workLock = new SemaphoreSlim(1, pipeline.MaxDegreeOfParallelism);

                while (await LetterBuffer.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    { return; }

                    try
                    {
                        while (LetterBuffer.Reader.TryRead(out var receivedLetter))
                        {
                            if (token.IsCancellationRequested)
                            { return; }

                            if (receivedLetter != null)
                            {
                                await workLock.WaitAsync().ConfigureAwait(false);

                                try
                                {
                                    await pipeline
                                        .QueueForExecutionAsync(receivedLetter)
                                        .ConfigureAwait(false);

                                    if (waitForCompletion)
                                    {
                                        await receivedLetter
                                            .Completion()
                                            .ConfigureAwait(false);
                                    }
                                }
                                finally
                                { workLock.Release(); }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            finally { pipeExecLock.Release(); }
        }
    }
}
