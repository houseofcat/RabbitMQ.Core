using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;

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

        private const string InitializeError = "AutoPublisher is not initialized or is shutdown.";
        private const string QueueChannelError = "Can't queue a letter to a closed Threading.Channel.";

        public AutoPublisher(Config config)
        {
            Config = config;
            Publisher = new Publisher(Config);
        }

        public AutoPublisher(ChannelPool channelPool)
        {
            Config = channelPool.Config;
            Publisher = new Publisher(channelPool);
        }

        public async ValueTask QueueLetterAsync(Letter letter, bool priority = false)
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(InitializeError);

            if (priority)
            {
                if (!await PriorityLetterQueue
                     .Writer
                     .WaitToWriteAsync()
                     .ConfigureAwait(false))
                {
                    throw new InvalidOperationException(QueueChannelError);
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
                    throw new InvalidOperationException(QueueChannelError);
                }

                await LetterQueue
                    .Writer
                    .WriteAsync(letter)
                    .ConfigureAwait(false);
            }
        }

        public async Task StartAsync()
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

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

            pubLock.Release();
        }

#if CORE2
        private async Task ProcessDeliveriesAsync(ChannelReader<Letter> channelReader)
        {
            while (await channelReader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (channelReader.TryRead(out var letter))
                {
                    await Publisher
                        .PublishAsync(letter, true)
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
                    await Publisher
                        .PublishAsync(letter, true)
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

            pubLock.Release();
        }

        public async Task SetProcessReceiptsAsync<TIn>(Func<PublishReceipt, TIn, Task> processReceiptAsync, TIn inputObject)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

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

            pubLock.Release();
        }
#elif CORE3
        public async Task SetProcessReceiptsAsync(Func<PublishReceipt, Task> processReceiptAsync)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);
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
            pubLock.Release();
        }

        public async Task SetProcessReceiptsAsync<TIn>(Func<PublishReceipt, TIn, Task> processReceiptAsync, TIn inputObject)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);
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
            pubLock.Release();
        }
#endif

        public async Task StopAsync(bool immediately = false)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

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

            pubLock.Release();
        }

        // TODO: Simplify usage. Add a memorycache failures for optional / automatic republish.
        public ChannelReader<PublishReceipt> GetReceiptBufferReader() => Publisher.ReceiptBuffer;
    }
}
