using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CookedRabbit.Core
{
    public interface IAutoPublisher
    {
        bool Compress { get; }
        Config Config { get; }
        bool CreatePublishReceipts { get; }
        bool Encrypt { get; }
        bool Initialized { get; }
        IPublisher Publisher { get; }
        bool Shutdown { get; }

        ChannelReader<PublishReceipt> GetReceiptBufferReader();
        ValueTask QueueLetterAsync(Letter letter, bool priority = false);
        Task SetProcessReceiptsAsync(Func<PublishReceipt, Task> processReceiptAsync);
        Task SetProcessReceiptsAsync<TIn>(Func<PublishReceipt, TIn, Task> processReceiptAsync, TIn inputObject);
        Task StartAsync(byte[] hashKey = null);
        Task StopAsync(bool immediately = false);
    }

    public class AutoPublisher : IAutoPublisher
    {
        private readonly ILogger<AutoPublisher> _logger;
        private readonly SemaphoreSlim _pubLock = new SemaphoreSlim(1, 1);
        private readonly bool _withHeaders;

        private Channel<Letter> _letterQueue;
        private Channel<Letter> _priorityLetterQueue;
        private byte[] _hashKey;
        private Task _publishingTask;
        private Task _publishingPriorityTask;
        private Task _processReceiptsAsync;

        public Config Config { get; }
        public IPublisher Publisher { get; }

        public bool Initialized { get; private set; }
        public bool Shutdown { get; private set; }

        public bool Compress { get; private set; }
        public bool Encrypt { get; private set; }
        public bool CreatePublishReceipts { get; private set; }

        public AutoPublisher(Config config, bool withHeaders = true)
        {
            Guard.AgainstNull(config, nameof(config));

            _logger = LogHelper.GetLogger<AutoPublisher>();
            Config = config;
            Publisher = new Publisher(Config);
            _withHeaders = withHeaders;
        }

        public AutoPublisher(IChannelPool channelPool, bool withHeaders = true)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));

            _logger = LogHelper.GetLogger<AutoPublisher>();
            Config = channelPool.Config;
            Publisher = new Publisher(channelPool);
            _withHeaders = withHeaders;
        }

        public async Task StartAsync(byte[] hashKey = null)
        {
            await _pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                CreatePublishReceipts = Config.PublisherSettings.CreatePublishReceipts;
                Compress = Config.PublisherSettings.Compress;
                Encrypt = Config.PublisherSettings.Encrypt;

                if (Encrypt && (hashKey == null || hashKey.Length != 32)) throw new InvalidOperationException(ExceptionMessages.EncrypConfigErrorMessage);
                _hashKey = hashKey;

                await Publisher.InitializeAsync().ConfigureAwait(false);

                _letterQueue = Channel.CreateBounded<Letter>(
                    new BoundedChannelOptions(Config.PublisherSettings.LetterQueueBufferSize)
                    {
                        FullMode = Config.PublisherSettings.BehaviorWhenFull
                    });
                _priorityLetterQueue = Channel.CreateBounded<Letter>(
                    new BoundedChannelOptions(Config.PublisherSettings.PriorityLetterQueueBufferSize)
                    {
                        FullMode = Config.PublisherSettings.BehaviorWhenFull
                    });

                _publishingTask = Task.Run(() => ProcessDeliveriesAsync(_letterQueue.Reader).ConfigureAwait(false));
                _publishingPriorityTask = Task.Run(() => ProcessDeliveriesAsync(_priorityLetterQueue.Reader).ConfigureAwait(false));

                Initialized = true;
                Shutdown = false;
            }
            finally { _pubLock.Release(); }
        }

        public async Task StopAsync(bool immediately = false)
        {
            await _pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                _letterQueue.Writer.Complete();
                _priorityLetterQueue.Writer.Complete();
                Publisher.ReceiptBuffer.Writer.Complete();

                if (!immediately)
                {
                    await _letterQueue
                        .Reader
                        .Completion
                        .ConfigureAwait(false);

                    await _priorityLetterQueue
                        .Reader
                        .Completion
                        .ConfigureAwait(false);

                    while (!_publishingTask.IsCompleted)
                    {
                        await Task.Delay(10).ConfigureAwait(false);
                    }

                    while (!_publishingPriorityTask.IsCompleted)
                    {
                        await Task.Delay(10).ConfigureAwait(false);
                    }
                }

                Shutdown = true;
            }
            finally
            { _pubLock.Release(); }
        }

        public ChannelReader<PublishReceipt> GetReceiptBufferReader() => Publisher.ReceiptBuffer.Reader;

        public async ValueTask QueueLetterAsync(Letter letter, bool priority = false)
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(ExceptionMessages.InitializeError);
            Guard.AgainstNull(letter, nameof(letter));

            if (priority)
            {
                if (!await _priorityLetterQueue
                     .Writer
                     .WaitToWriteAsync()
                     .ConfigureAwait(false))
                {
                    throw new InvalidOperationException(ExceptionMessages.QueueChannelError);
                }

                await _priorityLetterQueue
                    .Writer
                    .WriteAsync(letter)
                    .ConfigureAwait(false);
            }
            else
            {
                if (!await _letterQueue
                     .Writer
                     .WaitToWriteAsync()
                     .ConfigureAwait(false))
                {
                    throw new InvalidOperationException(ExceptionMessages.QueueChannelError);
                }

                _logger.LogDebug(LogMessages.AutoPublisher.LetterQueued, letter.LetterId, letter.LetterMetadata?.Id);

                await _letterQueue
                    .Writer
                    .WriteAsync(letter)
                    .ConfigureAwait(false);
            }
        }

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

                    if (Encrypt && (_hashKey != null || _hashKey.Length == 0))
                    {
                        letter.Body = AesEncrypt.Encrypt(letter.Body, _hashKey);
                        letter.LetterMetadata.Encrypted = Encrypt;
                    }

                    _logger.LogDebug(LogMessages.AutoPublisher.LetterPublished, letter.LetterId, letter.LetterMetadata?.Id);

                    await Publisher
                        .PublishAsync(letter, CreatePublishReceipts, _withHeaders)
                        .ConfigureAwait(false);
                }
            }
        }

        public async Task SetProcessReceiptsAsync(Func<PublishReceipt, Task> processReceiptAsync)
        {
            await _pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_processReceiptsAsync == null && processReceiptAsync != null)
                {
                    _processReceiptsAsync = Task.Run(async () =>
                    {
                        await foreach (var receipt in GetReceiptBufferReader().ReadAllAsync())
                        {
                            await processReceiptAsync(receipt).ConfigureAwait(false);
                        }
                    });
                }
            }
            finally { _pubLock.Release(); }
        }

        public async Task SetProcessReceiptsAsync<TIn>(Func<PublishReceipt, TIn, Task> processReceiptAsync, TIn inputObject)
        {
            await _pubLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_processReceiptsAsync == null && processReceiptAsync != null)
                {
                    _processReceiptsAsync = Task.Run(async () =>
                    {
                        await foreach (var receipt in GetReceiptBufferReader().ReadAllAsync())
                        {
                            await processReceiptAsync(receipt, inputObject).ConfigureAwait(false);
                        }
                    });
                }
            }
            finally { _pubLock.Release(); }
        }
    }
}
