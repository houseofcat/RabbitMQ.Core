using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Configs;
using CookedRabbit.Core.Pools;

namespace CookedRabbit.Core.Publisher
{
    public class AutoPublisher
    {
        private Config Config { get; }
        public Publisher Publisher { get; }
        private Channel<Letter> LetterQueue { get; set; }
        private Channel<Letter> PriorityLetterQueue { get; set; }
        private Task PublishingTask { get; set; }
        private Task PublishingPriorityTask { get; set; }

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

        public AutoPublisher(Publisher publisher)
        {
            Config = publisher.Config;
            Publisher = publisher;
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

            PublishingTask = Task.Run(async () => await ProcessDeliveriesAsync(LetterQueue.Reader).ConfigureAwait(false));
            PublishingPriorityTask = Task.Run(async () => await ProcessDeliveriesAsync(PriorityLetterQueue.Reader).ConfigureAwait(false));

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
                await Publisher
                    .PublisAsyncEnumerableAsync(channelReader.ReadAllAsync(), true)
                    .ConfigureAwait(false);
            }
        }
#endif

        public async Task StopAsync(bool immediately = false)
        {
            await pubLock.WaitAsync().ConfigureAwait(false);

            LetterQueue.Writer.Complete();
            PriorityLetterQueue.Writer.Complete();

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
    }
}
