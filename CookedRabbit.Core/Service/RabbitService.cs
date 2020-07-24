using CookedRabbit.Core.Configs;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using CookedRabbit.Core.WorkEngines;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Service
{
    public interface IRabbitService
    {
        IAutoPublisher AutoPublisher { get; }
        IChannelPool ChannelPool { get; }
        Config Config { get; }
        ConcurrentDictionary<string, ConsumerOptions> ConsumerPipelineSettings { get; }
        ConcurrentDictionary<string, IConsumer<ReceivedData>> Consumers { get; }
        bool Initialized { get; }
        ITopologer Topologer { get; }

        Task ComcryptAsync(Letter letter);
        Task<bool> CompressAsync(Letter letter);
        IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(string consumerName, int batchSize, bool? ensureOrdered, Func<int, bool?, IPipeline<ReceivedData, TOut>> pipelineBuilder) where TOut : IWorkState;
        IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(string consumerName, IPipeline<ReceivedData, TOut> pipeline) where TOut : IWorkState;
        Task DecomcryptAsync(Letter letter);
        Task<bool> DecompressAsync(Letter letter);
        bool Decrypt(Letter letter);
        bool Encrypt(Letter letter);
        Task<ReadOnlyMemory<byte>?> GetAsync(string queueName);
        Task<T> GetAsync<T>(string queueName);
        IConsumer<ReceivedData> GetConsumer(string consumerName);
        ConsumerPipelineOptions GetConsumerPipelineSettingsByConsumerName(string consumerName);
        ConsumerPipelineOptions GetConsumerPipelineSettingsByName(string consumerPipelineName);
        ConsumerOptions GetConsumerSettingsByPipelineName(string consumerPipelineName);
        Task InitializeAsync();
        Task InitializeAsync(string passphrase, string salt);
        ValueTask ShutdownAsync(bool immediately);
    }

    public class RabbitService : IRabbitService
    {
        private byte[] _hashKey { get; set; }
        private readonly SemaphoreSlim _serviceLock = new SemaphoreSlim(1, 1);

        public bool Initialized { get; private set; }
        public Config Config { get; }
        public IChannelPool ChannelPool { get; }
        public IAutoPublisher AutoPublisher { get; }
        public ITopologer Topologer { get; }

        public ConcurrentDictionary<string, IConsumer<ReceivedData>> Consumers { get; } = new ConcurrentDictionary<string, IConsumer<ReceivedData>>();
        public ConcurrentDictionary<string, ConsumerOptions> ConsumerPipelineSettings { get; } = new ConcurrentDictionary<string, ConsumerOptions>();

        /// <summary>
        /// Reads config from a provided file name path. Builds out a RabbitService with instantiated dependencies based on config settings.
        /// </summary>
        /// <param name="fileNamePath"></param>
        /// <param name="loggerFactory"></param>
        public RabbitService(string fileNamePath, ILoggerFactory loggerFactory = null)
        {
            LogHelper.LoggerFactory = loggerFactory;

            Config = ConfigReader
                .ConfigFileReadAsync(fileNamePath)
                .GetAwaiter()
                .GetResult();

            ChannelPool = new ChannelPool(Config);
            AutoPublisher = new AutoPublisher(ChannelPool);
            Topologer = new Topologer(ChannelPool);

            BuildConsumers();
        }

        /// <summary>
        /// Builds out a RabbitService with instantiated dependencies based on config settings.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="loggerFactory"></param>
        public RabbitService(Config config, ILoggerFactory loggerFactory = null)
        {
            LogHelper.LoggerFactory = loggerFactory;

            Config = config;
            ChannelPool = new ChannelPool(Config);
            AutoPublisher = new AutoPublisher(ChannelPool);
            Topologer = new Topologer(ChannelPool);

            BuildConsumers();
        }

        /// <summary>
        /// Use this constructor with DependencyInjection. Config's values are only used for RabbitService-esque settings and for building of Consumers.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="channelPool"></param>
        /// <param name="autoPublisher"></param>
        /// <param name="toploger"></param>
        /// <param name="loggerFactory"></param>
        public RabbitService(Config config, IChannelPool channelPool, IAutoPublisher autoPublisher, ITopologer toploger, ILoggerFactory loggerFactory = null)
        {
            LogHelper.LoggerFactory = loggerFactory;

            Config = config;
            ChannelPool = channelPool;
            AutoPublisher = autoPublisher;
            Topologer = toploger;

            BuildConsumers();
        }

        public async Task InitializeAsync()
        {
            await _serviceLock.WaitAsync().ConfigureAwait(false);

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

                    await FinishSettingUpConsumersAsync().ConfigureAwait(false);
                    Initialized = true;
                }
            }
            finally
            { _serviceLock.Release(); }
        }

        public async Task InitializeAsync(string passphrase, string salt)
        {
            await _serviceLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!Initialized)
                {
                    _hashKey = await ArgonHash
                        .GetHashKeyAsync(passphrase, salt, Constants.EncryptionKeySize)
                        .ConfigureAwait(false);

                    await ChannelPool
                        .InitializeAsync()
                        .ConfigureAwait(false);

                    await AutoPublisher
                        .StartAsync(_hashKey)
                        .ConfigureAwait(false);

                    await FinishSettingUpConsumersAsync().ConfigureAwait(false);
                    Initialized = true;
                }
            }
            finally
            { _serviceLock.Release(); }
        }

        public async ValueTask ShutdownAsync(bool immediately)
        {
            await _serviceLock.WaitAsync().ConfigureAwait(false);

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
            { _serviceLock.Release(); }
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
        }

        private void BuildConsumers()
        {
            foreach (var consumerSetting in Config.ConsumerSettings)
            {
                if (!string.IsNullOrWhiteSpace(consumerSetting.Value.GlobalSettings)
                    && Config.GlobalConsumerSettings.ContainsKey(consumerSetting.Value.GlobalSettings))
                {
                    var globalOverrides = Config.GlobalConsumerSettings[consumerSetting.Value.GlobalSettings];

                    consumerSetting.Value.NoLocal =
                        globalOverrides.NoLocal
                        ?? consumerSetting.Value.NoLocal;

                    consumerSetting.Value.Exclusive =
                        globalOverrides.Exclusive
                        ?? consumerSetting.Value.Exclusive;

                    consumerSetting.Value.BatchSize =
                        globalOverrides.BatchSize
                        ?? consumerSetting.Value.BatchSize;

                    consumerSetting.Value.AutoAck =
                        globalOverrides.AutoAck
                        ?? consumerSetting.Value.AutoAck;

                    consumerSetting.Value.UseTransientChannels =
                        globalOverrides.UseTransientChannels
                        ?? consumerSetting.Value.UseTransientChannels;

                    consumerSetting.Value.ErrorSuffix =
                        globalOverrides.ErrorSuffix
                        ?? consumerSetting.Value.ErrorSuffix;

                    consumerSetting.Value.BehaviorWhenFull =
                        globalOverrides.BehaviorWhenFull
                        ?? consumerSetting.Value.BehaviorWhenFull;

                    consumerSetting.Value.SleepOnIdleInterval =
                        globalOverrides.SleepOnIdleInterval
                        ?? consumerSetting.Value.SleepOnIdleInterval;

                    if (globalOverrides.GlobalConsumerPipelineSettings != null)
                    {
                        if (consumerSetting.Value.ConsumerPipelineSettings == null)
                        { consumerSetting.Value.ConsumerPipelineSettings = new Configs.ConsumerPipelineOptions(); }

                        consumerSetting.Value.ConsumerPipelineSettings.WaitForCompletion =
                            globalOverrides.GlobalConsumerPipelineSettings.WaitForCompletion
                            ?? consumerSetting.Value.ConsumerPipelineSettings.WaitForCompletion;

                        consumerSetting.Value.ConsumerPipelineSettings.MaxDegreesOfParallelism =
                            globalOverrides.GlobalConsumerPipelineSettings.MaxDegreesOfParallelism
                            ?? consumerSetting.Value.ConsumerPipelineSettings.MaxDegreesOfParallelism;

                        consumerSetting.Value.ConsumerPipelineSettings.EnsureOrdered =
                            globalOverrides.GlobalConsumerPipelineSettings.EnsureOrdered
                            ?? consumerSetting.Value.ConsumerPipelineSettings.EnsureOrdered;
                    }
                }

                if (!string.IsNullOrEmpty(consumerSetting.Value.ConsumerPipelineSettings.ConsumerPipelineName))
                {
                    ConsumerPipelineSettings.TryAdd(consumerSetting.Value.ConsumerPipelineSettings.ConsumerPipelineName, consumerSetting.Value);
                }
                Consumers.TryAdd(consumerSetting.Value.ConsumerName, new Consumer(ChannelPool, consumerSetting.Value, _hashKey));
            }
        }

        private async Task FinishSettingUpConsumersAsync()
        {
            foreach (var consumer in Consumers)
            {
                consumer.Value.HashKey = _hashKey;
                if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerSettings.QueueName))
                {
                    await Topologer.CreateQueueAsync(consumer.Value.ConsumerSettings.QueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerSettings.ErrorQueueName))
                {
                    await Topologer.CreateQueueAsync(consumer.Value.ConsumerSettings.ErrorQueueName).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(consumer.Value.ConsumerSettings.TargetQueueName))
                {
                    await Topologer.CreateQueueAsync(consumer.Value.ConsumerSettings.TargetQueueName).ConfigureAwait(false);
                }
            }
        }

        public IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(
            string consumerName,
            int batchSize,
            bool? ensureOrdered,
            Func<int, bool?, IPipeline<ReceivedData, TOut>> pipelineBuilder)
            where TOut : IWorkState
        {
            var consumer = GetConsumer(consumerName);
            var pipeline = pipelineBuilder.Invoke(batchSize, ensureOrdered);

            return new ConsumerPipeline<TOut>(consumer, pipeline);
        }

        public IConsumerPipeline<TOut> CreateConsumerPipeline<TOut>(
            string consumerName,
            IPipeline<ReceivedData, TOut> pipeline)
            where TOut : IWorkState
        {
            var consumer = GetConsumer(consumerName);

            return new ConsumerPipeline<TOut>(consumer, pipeline);
        }

        public IConsumer<ReceivedData> GetConsumer(string consumerName)
        {
            if (!Consumers.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerSettingsMessage, consumerName));
            return Consumers[consumerName];
        }

        public ConsumerOptions GetConsumerSettingsByPipelineName(string consumerPipelineName)
        {
            if (!ConsumerPipelineSettings.ContainsKey(consumerPipelineName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerPipelineSettingsMessage, consumerPipelineName));
            return ConsumerPipelineSettings[consumerPipelineName];
        }

        public ConsumerPipelineOptions GetConsumerPipelineSettingsByName(string consumerPipelineName)
        {
            if (!ConsumerPipelineSettings.ContainsKey(consumerPipelineName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerPipelineSettingsMessage, consumerPipelineName));
            return ConsumerPipelineSettings[consumerPipelineName].ConsumerPipelineSettings;
        }

        public ConsumerPipelineOptions GetConsumerPipelineSettingsByConsumerName(string consumerName)
        {
            if (!Consumers.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerSettingsMessage, consumerName));
            return Consumers[consumerName].ConsumerSettings.ConsumerPipelineSettings;
        }

        public async Task DecomcryptAsync(Letter letter)
        {
            var decryptFailed = false;
            if (letter.LetterMetadata.Encrypted && (_hashKey?.Length > 0))
            {
                try
                {
                    letter.Body = AesEncrypt.Decrypt(letter.Body, _hashKey);
                    letter.LetterMetadata.Encrypted = false;
                }
                catch { decryptFailed = true; }
            }

            if (!decryptFailed && letter.LetterMetadata.Compressed)
            {
                try
                {
                    letter.Body = await Gzip.DecompressAsync(letter.Body).ConfigureAwait(false);
                    letter.LetterMetadata.Compressed = false;
                }
                catch { }
            }
        }

        public async Task ComcryptAsync(Letter letter)
        {
            if (letter.LetterMetadata.Compressed)
            {
                try
                {
                    letter.Body = await Gzip.CompressAsync(letter.Body).ConfigureAwait(false);
                    letter.LetterMetadata.Compressed = true;
                }
                catch { }
            }

            if (!letter.LetterMetadata.Encrypted && (_hashKey?.Length > 0))
            {
                try
                {
                    letter.Body = AesEncrypt.Encrypt(letter.Body, _hashKey);
                    letter.LetterMetadata.Encrypted = true;
                }
                catch { }
            }
        }

        public bool Encrypt(Letter letter)
        {
            if (!letter.LetterMetadata.Encrypted && (_hashKey?.Length > 0))
            {
                try
                {
                    letter.Body = AesEncrypt.Encrypt(letter.Body, _hashKey);
                    letter.LetterMetadata.Encrypted = true;
                }
                catch { }
            }

            return letter.LetterMetadata.Encrypted;
        }

        public bool Decrypt(Letter letter)
        {
            if (letter.LetterMetadata.Encrypted && (_hashKey?.Length > 0))
            {
                try
                {
                    letter.Body = AesEncrypt.Decrypt(letter.Body, _hashKey);
                    letter.LetterMetadata.Encrypted = false;
                }
                catch { }
            }

            return !letter.LetterMetadata.Encrypted;
        }

        public async Task<bool> CompressAsync(Letter letter)
        {
            if (letter.LetterMetadata.Encrypted)
            { return false; }

            if (!letter.LetterMetadata.Compressed)
            {
                try
                {
                    letter.Body = await Gzip.CompressAsync(letter.Body).ConfigureAwait(false);
                    letter.LetterMetadata.Compressed = true;
                }
                catch { }
            }

            return letter.LetterMetadata.Compressed;
        }

        public async Task<bool> DecompressAsync(Letter letter)
        {
            if (letter.LetterMetadata.Encrypted)
            { return false; }

            if (letter.LetterMetadata.Compressed)
            {
                try
                {
                    letter.Body = await Gzip.DecompressAsync(letter.Body).ConfigureAwait(false);
                    letter.LetterMetadata.Compressed = false;
                }
                catch { }
            }

            return !letter.LetterMetadata.Compressed;
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
