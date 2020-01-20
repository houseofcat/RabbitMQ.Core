using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;

namespace CookedRabbit.Core.Service
{
    public class RabbitService : IRabbitService
    {
        private Config Config { get; }
        public bool Initialized { get; private set; }
        private readonly SemaphoreSlim serviceLock = new SemaphoreSlim(1, 1);

        public ChannelPool ChannelPool { get; }
        public AutoPublisher AutoPublisher { get; }
        public Topologer Topologer { get; }
        public ConcurrentDictionary<string, LetterConsumer> LetterConsumers { get; }
        public ConcurrentDictionary<string, MessageConsumer> MessageConsumers { get; }

        private byte[] HashKey { get; set; }
        private const int KeySize = 32;

        public RabbitService(string fileNamePath)
        {
            Config = ConfigReader.ConfigFileRead(fileNamePath);
            ChannelPool = new ChannelPool(Config);
            AutoPublisher = new AutoPublisher(ChannelPool);
            Topologer = new Topologer(ChannelPool);
            LetterConsumers = new ConcurrentDictionary<string, LetterConsumer>();
            MessageConsumers = new ConcurrentDictionary<string, MessageConsumer>();
        }

        public RabbitService(Config config)
        {
            Config = config;
            ChannelPool = new ChannelPool(Config);
            AutoPublisher = new AutoPublisher(ChannelPool);
            Topologer = new Topologer(ChannelPool);
            LetterConsumers = new ConcurrentDictionary<string, LetterConsumer>();
            MessageConsumers = new ConcurrentDictionary<string, MessageConsumer>();
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

                    BuildConsumers();
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

                    BuildConsumers();
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

        private void BuildConsumers()
        {
            foreach (var consumerSetting in Config.LetterConsumerSettings)
            {
                LetterConsumers.TryAdd(consumerSetting.Value.ConsumerName, new LetterConsumer(ChannelPool, consumerSetting.Value, HashKey));
            }

            foreach (var consumerSetting in Config.MessageConsumerSettings)
            {
                MessageConsumers.TryAdd(consumerSetting.Value.ConsumerName, new MessageConsumer(ChannelPool, consumerSetting.Value));
            }
        }

        public LetterConsumer GetLetterConsumer(string consumerName)
        {
            if (!LetterConsumers.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Strings.NoConsumerSettingsMessage, consumerName));
            return LetterConsumers[consumerName];
        }

        public MessageConsumer GetMessageConsumer(string consumerName)
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
    }
}
