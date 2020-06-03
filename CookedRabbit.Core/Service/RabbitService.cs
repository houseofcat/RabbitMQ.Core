using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using RabbitMQ.Client;

namespace CookedRabbit.Core.Service
{
    public interface IRabbitService
    {
        IAutoPublisher AutoPublisher { get; }
        IChannelPool ChannelPool { get; }
        bool Initialized { get; }
        ConcurrentDictionary<string, IConsumer<ReceivedData>> Consumers { get; }
        ConcurrentDictionary<string, IConsumer<ReceivedLetter>> LetterConsumers { get; }
        ConcurrentDictionary<string, IConsumer<ReceivedMessage>> MessageConsumers { get; }
        ITopologer Topologer { get; }

        Task ComcryptAsync(ReceivedLetter receivedLetter);
        Task<bool> CompressAsync(ReceivedLetter receivedLetter);
        Task DecomcryptAsync(ReceivedLetter receivedLetter);
        Task<bool> DecompressAsync(ReceivedLetter receivedLetter);
        bool Decrypt(ReceivedLetter receivedLetter);
        bool Encrypt(ReceivedLetter receivedLetter);
        Task<ReadOnlyMemory<byte>?> GetAsync(string queueName);
        Task<T> GetAsync<T>(string queueName);
        IConsumer<ReceivedData> GetConsumer(string consumerName);
        IConsumer<ReceivedLetter> GetLetterConsumer(string consumerName);
        IConsumer<ReceivedMessage> GetMessageConsumer(string consumerName);
        Task InitializeAsync();
        Task InitializeAsync(string passphrase, string salt);
        ValueTask ShutdownAsync(bool immediately);
    }

    public class RabbitService : IRabbitService
    {
        private Config Config { get; }
        public bool Initialized { get; private set; }
        private readonly SemaphoreSlim serviceLock = new SemaphoreSlim(1, 1);

        public IChannelPool ChannelPool { get; }
        public IAutoPublisher AutoPublisher { get; }
        public ITopologer Topologer { get; }

        public ConcurrentDictionary<string, IConsumer<ReceivedData>> Consumers { get; } = new ConcurrentDictionary<string, IConsumer<ReceivedData>>();
        public ConcurrentDictionary<string, IConsumer<ReceivedLetter>> LetterConsumers { get; } = new ConcurrentDictionary<string, IConsumer<ReceivedLetter>>();
        public ConcurrentDictionary<string, IConsumer<ReceivedMessage>> MessageConsumers { get; } = new ConcurrentDictionary<string, IConsumer<ReceivedMessage>>();

        private byte[] HashKey { get; set; }
        private const int KeySize = 32;

        /// <summary>
        /// Reads config from a provided file name path. Builds out a RabbitService with instantiated dependencies based on config settings.
        /// </summary>
        /// <param name="fileNamePath"></param>
        public RabbitService(string fileNamePath)
        {
            Config = ConfigReader
                .ConfigFileReadAsync(fileNamePath)
                .GetAwaiter()
                .GetResult();

            ChannelPool = new ChannelPool(Config);
            AutoPublisher = new AutoPublisher(ChannelPool);
            Topologer = new Topologer(ChannelPool);
        }

        /// <summary>
        /// Builds out a RabbitService with instantiated dependencies based on config settings.
        /// </summary>
        /// <param name="config"></param>
        public RabbitService(Config config)
        {
            Config = config;
            ChannelPool = new ChannelPool(Config);
            AutoPublisher = new AutoPublisher(ChannelPool);
            Topologer = new Topologer(ChannelPool);
        }

        /// <summary>
        /// Use this constructor with DependencyInjection. Config's values are only used for RabbitService-esque settings and for building of Consumers.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="channelPool"></param>
        /// <param name="autoPublisher"></param>
        /// <param name="toploger"></param>
        public RabbitService(Config config, IChannelPool channelPool, IAutoPublisher autoPublisher, ITopologer toploger)
        {
            Config = config;
            ChannelPool = channelPool;
            AutoPublisher = autoPublisher;
            Topologer = toploger;
        }

        public async Task InitializeAsync()
        {
            await serviceLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!Initialized)
                {
                    await ChannelPool
                        .InitializeAsync()
                        .ConfigureAwait(false);

                    await AutoPublisher
                        .StartAsync()
                        .ConfigureAwait(false);

                    await BuildConsumersAsync().ConfigureAwait(false);
                    Initialized = true;
                }
            }
            finally
            { serviceLock.Release(); }
        }

        public async Task InitializeAsync(string passphrase, string salt)
        {
            await serviceLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!Initialized)
                {
                    HashKey = await ArgonHash
                        .GetHashKeyAsync(passphrase, salt, KeySize)
                        .ConfigureAwait(false);

                    await ChannelPool
                        .InitializeAsync()
                        .ConfigureAwait(false);

                    await AutoPublisher
                        .StartAsync(HashKey)
                        .ConfigureAwait(false);

                    await BuildConsumersAsync().ConfigureAwait(false);
                    Initialized = true;
                }
            }
            finally
            { serviceLock.Release(); }
        }

        public async ValueTask ShutdownAsync(bool immediately)
        {
            await serviceLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await AutoPublisher
                    .StopAsync(immediately)
                    .ConfigureAwait(false);

                await StopAllConsumers(immediately)
                    .ConfigureAwait(false);

                await ChannelPool
                    .ShutdownAsync()
                    .ConfigureAwait(false);
            }
            finally
            { serviceLock.Release(); }
        }

        private async ValueTask StopAllConsumers(bool immediately)
        {
            foreach (var kvp in Consumers)
            {
                await kvp
                    .Value
                    .StopConsumerAsync(immediately)
                    .ConfigureAwait(false);
            }

            foreach (var kvp in LetterConsumers)
            {
                await kvp
                    .Value
                    .StopConsumerAsync(immediately)
                    .ConfigureAwait(false);
            }

            foreach (var kvp in MessageConsumers)
            {
                await kvp
                    .Value
                    .StopConsumerAsync(immediately)
                    .ConfigureAwait(false);
            }
        }

        private async Task BuildConsumersAsync()
        {
            foreach (var consumerSetting in Config.ConsumerSettings)
            {
                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.QueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.QueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.ErrorQueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.ErrorQueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.TargetQueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.TargetQueueName).ConfigureAwait(false);
                }

                Consumers.TryAdd(consumerSetting.Value.ConsumerName, new Consumer(ChannelPool, consumerSetting.Value, HashKey));
            }

            foreach (var consumerSetting in Config.LetterConsumerSettings)
            {
                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.QueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.QueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.ErrorQueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.ErrorQueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.TargetQueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.TargetQueueName).ConfigureAwait(false);
                }

                LetterConsumers.TryAdd(consumerSetting.Value.ConsumerName, new LetterConsumer(ChannelPool, consumerSetting.Value, HashKey));
            }

            foreach (var consumerSetting in Config.MessageConsumerSettings)
            {
                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.QueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.QueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.ErrorQueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.ErrorQueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.TargetQueueName))
                {
                    await Topologer.CreateQueueAsync(consumerSetting.Value.TargetQueueName).ConfigureAwait(false);
                }

                MessageConsumers.TryAdd(consumerSetting.Value.ConsumerName, new MessageConsumer(ChannelPool, consumerSetting.Value));
            }
        }

        public IConsumer<ReceivedData> GetConsumer(string consumerName)
        {
            if (!Consumers.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Strings.NoConsumerSettingsMessage, consumerName));
            return Consumers[consumerName];
        }

        public IConsumer<ReceivedLetter> GetLetterConsumer(string consumerName)
        {
            if (!LetterConsumers.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Strings.NoConsumerSettingsMessage, consumerName));
            return LetterConsumers[consumerName];
        }

        public IConsumer<ReceivedMessage> GetMessageConsumer(string consumerName)
        {
            if (!MessageConsumers.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Strings.NoConsumerSettingsMessage, consumerName));
            return MessageConsumers[consumerName];
        }

        public async Task DecomcryptAsync(ReceivedLetter receivedLetter)
        {
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
        }

        public async Task ComcryptAsync(ReceivedLetter receivedLetter)
        {
            if (receivedLetter.Letter.LetterMetadata.Compressed)
            {
                try
                {
                    receivedLetter.Letter.Body = await Gzip.CompressAsync(receivedLetter.Letter.Body).ConfigureAwait(false);
                    receivedLetter.Letter.LetterMetadata.Compressed = true;
                }
                catch { }
            }

            if (!receivedLetter.Letter.LetterMetadata.Encrypted && (HashKey?.Length > 0))
            {
                try
                {
                    receivedLetter.Letter.Body = AesEncrypt.Encrypt(receivedLetter.Letter.Body, HashKey);
                    receivedLetter.Letter.LetterMetadata.Encrypted = true;
                }
                catch { }
            }
        }

        public bool Encrypt(ReceivedLetter receivedLetter)
        {
            if (!receivedLetter.Letter.LetterMetadata.Encrypted && (HashKey?.Length > 0))
            {
                try
                {
                    receivedLetter.Letter.Body = AesEncrypt.Encrypt(receivedLetter.Letter.Body, HashKey);
                    receivedLetter.Letter.LetterMetadata.Encrypted = true;
                }
                catch { }
            }

            return receivedLetter.Letter.LetterMetadata.Encrypted;
        }

        public bool Decrypt(ReceivedLetter receivedLetter)
        {
            if (receivedLetter.Letter.LetterMetadata.Encrypted && (HashKey?.Length > 0))
            {
                try
                {
                    receivedLetter.Letter.Body = AesEncrypt.Decrypt(receivedLetter.Letter.Body, HashKey);
                    receivedLetter.Letter.LetterMetadata.Encrypted = false;
                }
                catch { }
            }

            return !receivedLetter.Letter.LetterMetadata.Encrypted;
        }

        public async Task<bool> CompressAsync(ReceivedLetter receivedLetter)
        {
            if (receivedLetter.Letter.LetterMetadata.Encrypted)
            { return false; }

            if (!receivedLetter.Letter.LetterMetadata.Compressed)
            {
                try
                {
                    receivedLetter.Letter.Body = await Gzip.CompressAsync(receivedLetter.Letter.Body).ConfigureAwait(false);
                    receivedLetter.Letter.LetterMetadata.Compressed = true;
                }
                catch { }
            }

            return receivedLetter.Letter.LetterMetadata.Compressed;
        }

        public async Task<bool> DecompressAsync(ReceivedLetter receivedLetter)
        {
            if (receivedLetter.Letter.LetterMetadata.Encrypted)
            { return false; }

            if (receivedLetter.Letter.LetterMetadata.Compressed)
            {
                try
                {
                    receivedLetter.Letter.Body = await Gzip.DecompressAsync(receivedLetter.Letter.Body).ConfigureAwait(false);
                    receivedLetter.Letter.LetterMetadata.Compressed = false;
                }
                catch { }
            }

            return !receivedLetter.Letter.LetterMetadata.Compressed;
        }

        /// <summary>
        /// Simple retrieve message (byte[]) from queue. Null if nothing was available or on error.
        /// <para>AutoAcks message.</para>
        /// </summary>
        /// <param name="queueName"></param>
        public async Task<ReadOnlyMemory<byte>?> GetAsync(string queueName)
        {
            IChannelHost chanHost;

            try
            {
                chanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            catch { return default; }

            BasicGetResult result = null;
            var error = false;
            try
            {
                result = chanHost
                    .Channel
                    .BasicGet(queueName, true);
            }
            catch { error = true; }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(chanHost, error)
                    .ConfigureAwait(false);
            }

            return result?.Body;
        }

        /// <summary>
        /// Simple retrieve message (byte[]) from queue and convert to type T. Default (assumed null) if nothing was available (or on transmission error).
        /// <para>AutoAcks message.</para>
        /// </summary>
        /// <param name="queueName"></param>
        public async Task<T> GetAsync<T>(string queueName)
        {
            IChannelHost chanHost;

            try
            {
                chanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            catch { return default; }

            BasicGetResult result = null;
            var error = false;
            try
            {
                result = chanHost
                    .Channel
                    .BasicGet(queueName, true);
            }
            catch { error = true; }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(chanHost, error)
                    .ConfigureAwait(false);
            }

            return result != null ? JsonSerializer.Deserialize<T>(result.Body.Span) : default;
        }
    }
}
