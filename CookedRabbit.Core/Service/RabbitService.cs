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
        private ConcurrentDictionary<string, LetterConsumer> Consumers { get; }

        private byte[] HashKey { get; set; }
        private const int KeySize = 32;

        public RabbitService(string fileNamePath)
        {
            Config = ConfigReader.ConfigFileRead(fileNamePath);
            ChannelPool = new ChannelPool(Config);
            AutoPublisher = new AutoPublisher(ChannelPool);
            Topologer = new Topologer(ChannelPool);
            Consumers = new ConcurrentDictionary<string, LetterConsumer>();
        }

        public RabbitService(Config config)
        {
            Config = config;
            ChannelPool = new ChannelPool(Config);
            AutoPublisher = new AutoPublisher(ChannelPool);
            Topologer = new Topologer(ChannelPool);
            Consumers = new ConcurrentDictionary<string, LetterConsumer>();
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

        private void BuildConsumers()
        {
            foreach (var consumerSetting in Config.ConsumerSettings)
            {
                Consumers.TryAdd(consumerSetting.Value.ConsumerName, new LetterConsumer(ChannelPool, consumerSetting.Value, HashKey));
            }
        }

        public LetterConsumer GetConsumer(string consumerName)
        {
            if (!Consumers.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Strings.NoConsumerSettingsMessage, consumerName));
            return Consumers[consumerName];
        }
    }
}
