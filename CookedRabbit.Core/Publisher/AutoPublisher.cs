using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;

namespace CookedRabbit.Core
{
    public class AutoPublisher
    {
        private Config Config { get; }
        public Publisher Publisher { get; }
        private Channel<Letter> LetterQueue { get; set; }
        private Channel<Letter> PriorityLetterQueue { get; set; }
        private Task PublishingTask { get; set; }
        private Task PublishingPriorityTask { get; set; }
        private Task ProcessReceiptsAsync { get; set; }

        private readonly SemaphoreSlim pubLock = new SemaphoreSlim(1, 1);

        public bool Initialized { get; private set; }
        public bool Shutdown { get; private set; }

        public bool Compress { get; private set; }
        public bool Encrypt { get; private set; }
        public bool CreatePublishReceipts { get; private set; }
        private byte[] HashKey { get; set; }

        public AutoPublisher(Config config)
        {
            Guard.AgainstNull(config, nameof(config));

            Config = config;
            Publisher = new Publisher(Config);
        }

        public AutoPublisher(ChannelPool channelPool)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));

            Config = channelPool.Config;
            Publisher = new Publisher(channelPool);
        }

        public async Task StartAsync(byte[] hashKey = null)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                CreatePublishReceipts = Config.PublisherSettings.CreatePublishReceipts;
                Compress = Config.PublisherSettings.Compress;
                Encrypt = Config.PublisherSettings.Encrypt;

                if (Encrypt && (hashKey == null || hashKey.Length != 32)) throw new InvalidOperationException(Strings.EncrypConfigErrorMessage);
                HashKey = hashKey;

                await Publisher.ChannelPool.InitializeAsync().ConfigureAwait(false);

                LetterQueue = Channel.CreateBounded<Letter>(
                    new BoundedChannelOptions(Config.PublisherSettings.LetterQueueBufferSize)
                    {
                        FullMode = Config.PublisherSettings.BehaviorWhenFull
                    });
                PriorityLetterQueue = Channel.CreateBounded<Letter>(
                    new BoundedChannelOptions(Config.PublisherSettings.PriorityLetterQueueBufferSize)
                    {
                        FullMode = Config.PublisherSettings.BehaviorWhenFull
                    });

                PublishingTask = Task.Run(() => ProcessDeliveriesAsync(LetterQueue.Reader).ConfigureAwait(false));
                PublishingPriorityTask = Task.Run(() => ProcessDeliveriesAsync(PriorityLetterQueue.Reader).ConfigureAwait(false));

                Initialized = true;
                Shutdown = false;
            }
            finally { pubLock.Release(); }
        }

        public async Task StopAsync(bool immediately = false)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                LetterQueue.Writer.Complete();
                PriorityLetterQueue.Writer.Complete();
                Publisher.ReceiptBuffer.Writer.Complete();

                if (!immediately)
                {
                    await LetterQueue
                        .Reader
                        .Completion
                        .ConfigureAwait(false);

                    await PriorityLetterQueue
                        .Reader
                        .Completion
                        .ConfigureAwait(false);

                    while (!PublishingTask.IsCompleted)
                    {
                        await Task.Delay(10).ConfigureAwait(false);
                    }

                    while (!PublishingPriorityTask.IsCompleted)
                    {
                        await Task.Delay(10).ConfigureAwait(false);
                    }
                }

                Shutdown = true;
            }
            finally
            { pubLock.Release(); }
        }

        // TODO: Simplify usage. Add a memorycache failures for optional / automatic republish.
        public ChannelReader<PublishReceipt> GetReceiptBufferReader() => Publisher.ReceiptBuffer;

        public async ValueTask QueueLetterAsync(Letter letter, bool priority = false)
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(Strings.InitializeError);

            if (priority)
            {
                if (!await PriorityLetterQueue
                     .Writer
                     .WaitToWriteAsync()
                     .ConfigureAwait(false))
                {
                    throw new InvalidOperationException(Strings.QueueChannelError);
                }

                await PriorityLetterQueue
                    .Writer
                    .WriteAsync(letter)
                    .ConfigureAwait(false);
            }
            else
            {
                if (!await LetterQueue
                     .Writer
                     .WaitToWriteAsync()
                     .ConfigureAwait(false))
                {
                    throw new InvalidOperationException(Strings.QueueChannelError);
                }

                await LetterQueue
                    .Writer
                    .WriteAsync(letter)
                    .ConfigureAwait(false);
            }
        }

#if CORE2
        private async Task ProcessDeliveriesAsync(ChannelReader<Letter> channelReader)
        {
            while (await channelReader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (channelReader.TryRead(out var letter))
                {
                    if (letter == null)
                    { continue; }

                    if (Compress)
                    {
                        letter.Body = await Gzip.CompressAsync(letter.Body).ConfigureAwait(false);
                        letter.LetterMetadata.Compressed = Compress;
                    }

                    if (Encrypt && (HashKey != null || HashKey.Length == 0))
                    {
                        letter.Body = AesEncrypt.Encrypt(letter.Body, HashKey);
                        letter.LetterMetadata.Encrypted = Encrypt;
                    }

                    await Publisher
                        .PublishAsync(letter, CreatePublishReceipts)
                        .ConfigureAwait(false);
                }
            }
        }
#elif CORE3
        private async Task ProcessDeliveriesAsync(ChannelReader<Letter> channelReader)
        {
            while (await channelReader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (channelReader.TryRead(out var letter))
                {
                    if (letter == null)
                    { continue; }

                    if (Compress)
                    {
                        letter.Body = await Gzip.CompressAsync(letter.Body).ConfigureAwait(false);
                        letter.LetterMetadata.Compressed = Compress;
                    }

                    if (Encrypt && (HashKey != null || HashKey.Length == 0))
                    {
                        letter.Body = AesEncrypt.Encrypt(letter.Body, HashKey);
                        letter.LetterMetadata.Encrypted = Encrypt;
                    }

                    await Publisher
                        .PublishAsync(letter, CreatePublishReceipts)
                        .ConfigureAwait(false);
                }
            }
            //while (await channelReader.WaitToReadAsync().ConfigureAwait(false))
            //{
            //    await Publisher
            //        .PublishAsyncEnumerableAsync(channelReader.ReadAllAsync(), true)
            //        .ConfigureAwait(false);
            //}
        }
#endif

#if CORE2
        public async Task SetProcessReceiptsAsync(Func<PublishReceipt, Task> processReceiptAsync)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (ProcessReceiptsAsync == null && processReceiptAsync != null)
                {
                    ProcessReceiptsAsync = Task.Run(async () =>
                    {
                        while (await GetReceiptBufferReader().WaitToReadAsync().ConfigureAwait(false))
                        {
                            while (GetReceiptBufferReader().TryRead(out var receipt))
                            {
                                await processReceiptAsync(receipt).ConfigureAwait(false);
                            }
                        }
                    });
                }
            }
            finally
            { pubLock.Release(); }
        }

        public async Task SetProcessReceiptsAsync<TIn>(Func<PublishReceipt, TIn, Task> processReceiptAsync, TIn inputObject) where TIn : new()
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (ProcessReceiptsAsync == null && processReceiptAsync != null)
                {
                    ProcessReceiptsAsync = Task.Run(async () =>
                    {
                        while (await GetReceiptBufferReader().WaitToReadAsync().ConfigureAwait(false))
                        {
                            while (GetReceiptBufferReader().TryRead(out var receipt))
                            {
                                await processReceiptAsync(receipt, inputObject).ConfigureAwait(false);
                            }
                        }
                    });
                }
            }
            finally
            { pubLock.Release(); }
        }
#elif CORE3
        public async Task SetProcessReceiptsAsync(Func<PublishReceipt, Task> processReceiptAsync)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (ProcessReceiptsAsync == null && processReceiptAsync != null)
                {
                    ProcessReceiptsAsync = Task.Run(async () =>
                    {
                        await foreach (var receipt in GetReceiptBufferReader().ReadAllAsync())
                        {
                            await processReceiptAsync(receipt).ConfigureAwait(false);
                        }
                    });
                }
            }
            finally { pubLock.Release(); }
        }

        public async Task SetProcessReceiptsAsync<TIn>(Func<PublishReceipt, TIn, Task> processReceiptAsync, TIn inputObject)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (ProcessReceiptsAsync == null && processReceiptAsync != null)
                {
                    ProcessReceiptsAsync = Task.Run(async () =>
                    {
                        await foreach (var receipt in GetReceiptBufferReader().ReadAllAsync())
                        {
                            await processReceiptAsync(receipt, inputObject).ConfigureAwait(false);
                        }
                    });
                }
            }
            finally { pubLock.Release(); }
        }
#endif
    }
}
