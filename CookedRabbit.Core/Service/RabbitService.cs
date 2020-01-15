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
                if (consumerSetting.Value.Enabled)
                {
                    LetterConsumers.TryAdd(consumerSetting.Value.ConsumerName, new LetterConsumer(ChannelPool, consumerSetting.Value, HashKey));
                }
            }

            foreach (var consumerSetting in Config.MessageConsumerSettings)
            {
                if (consumerSetting.Value.Enabled)
                {
                    MessageConsumers.TryAdd(consumerSetting.Value.ConsumerName, new MessageConsumer(ChannelPool, consumerSetting.Value));
                }
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
    }
}
