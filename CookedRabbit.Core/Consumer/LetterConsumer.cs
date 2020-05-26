using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using CookedRabbit.Core.WorkEngines;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

    public class LetterConsumer : IConsumer<ReceivedLetter>
    {
        public Config Config { get; }

        public IChannelPool ChannelPool { get; }
        public Channel<ReceivedLetter> DataBuffer { get; private set; }

        private IChannelHost ConsumingChannelHost { get; set; }

        public ConsumerOptions ConsumerSettings { get; set; }

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

            ConsumerSettings = Config.GetLetterConsumerSettings(consumerName);
        }

        public LetterConsumer(IChannelPool channelPool, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

            ConsumerSettings = Config.GetLetterConsumerSettings(consumerName);
        }

        public LetterConsumer(IChannelPool channelPool, ConsumerOptions consumerSettings, byte[] hashKey = null)
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
            ConsumerSettings = Config.GetLetterConsumerSettings(ConsumerSettings.ConsumerName);

            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (ConsumerSettings.Enabled && !Consuming)
                {
                    AutoAck = autoAck;
                    UseTransientChannel = useTransientChannel;

                    DataBuffer = Channel.CreateBounded<ReceivedLetter>(
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

            if (await DataBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false))
            {
                await DataBuffer
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
                ConsumingChannelHost.Channel.BasicQos(0, ConsumerSettings.QosPrefetchCount, false);
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

            if (await DataBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                await DataBuffer
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

        public ChannelReader<ReceivedLetter> GetConsumerBuffer() => DataBuffer.Reader;

        public async ValueTask<ReceivedLetter> ReadAsync()
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

        public async Task<IEnumerable<ReceivedLetter>> ReadUntilEmptyAsync()
        {
            if (!await DataBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            var list = new List<ReceivedLetter>();
            await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (DataBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                list.Add(message);
            }

            return list;
        }

        public async IAsyncEnumerable<ReceivedLetter> StreamOutUntilEmptyAsync()
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

        public async IAsyncEnumerable<ReceivedLetter> StreamOutUntilClosedAsync()
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

        public async Task ExecutionEngineAsync(Func<ReceivedLetter, Task> workAsync)
        {
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedLetter in DataBuffer.Reader.ReadAllAsync())
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
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedLetter in DataBuffer.Reader.ReadAllAsync())
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
            while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                await foreach (var receivedLetter in DataBuffer.Reader.ReadAllAsync())
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

        private readonly SemaphoreSlim parallelExecLock = new SemaphoreSlim(1, 1);

        public async Task ParallelExecutionEngineAsync(Func<ReceivedLetter, Task<bool>> workAsync, int maxDoP = 4, CancellationToken token = default)
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
                        while (DataBuffer.Reader.TryRead(out var receivedLetter))
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

        public async Task DataflowExecutionEngineAsync(Func<ReceivedLetter, Task<bool>> workBodyAsync, int maxDoP = 4, CancellationToken token = default)
        {
            await dataFlowExecLock.WaitAsync(2000).ConfigureAwait(false);

            try
            {
                if (token.IsCancellationRequested)
                { return; }

                var letterEngine = new LetterDataflowEngine(workBodyAsync, maxDoP);

                while (await DataBuffer.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    { return; }

                    try
                    {
                        while (DataBuffer.Reader.TryRead(out var receivedLetter))
                        {
                            if (receivedLetter != null)
                            {
                                await letterEngine
                                    .EnqueueWorkAsync(receivedLetter)
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

        public async Task PipelineExecutionEngineAsync<TOut>(IPipeline<ReceivedLetter, TOut> pipeline, bool waitForCompletion, CancellationToken token = default)
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
                        while (DataBuffer.Reader.TryRead(out var receivedLetter))
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
