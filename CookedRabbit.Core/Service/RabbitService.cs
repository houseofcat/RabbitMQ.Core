using CookedRabbit.Core.Configs;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using CookedRabbit.Core.WorkEngines;
using Microsoft.Extensions.Logging;
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
        IPublisher Publisher { get; }
        IChannelPool ChannelPool { get; }
        Config Config { get; }
        ConcurrentDictionary<string, IConsumer<ReceivedData>> Consumers { get; }
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
        IConsumer<ReceivedData> GetConsumerByPipelineName(string consumerPipelineName);

        ValueTask ShutdownAsync(bool immediately);
    }

    public class RabbitService : IRabbitService
    {
        private byte[] _hashKey { get; set; }
        private readonly SemaphoreSlim _serviceLock = new SemaphoreSlim(1, 1);

        public Config Config { get; }
        public IChannelPool ChannelPool { get; }
        public IPublisher Publisher { get; }
        public ITopologer Topologer { get; }

        public ConcurrentDictionary<string, IConsumer<ReceivedData>> Consumers { get; } = new ConcurrentDictionary<string, IConsumer<ReceivedData>>();
        private ConcurrentDictionary<string, ConsumerOptions> ConsumerPipelineNameToConsumerSetting { get; } = new ConcurrentDictionary<string, ConsumerOptions>();

        /// <summary>
        /// Reads config from a provided file name path. Builds out a RabbitService with instantiated dependencies based on config settings.
        /// </summary>
        /// <param name="fileNamePath"></param>
        /// <param name="loggerFactory"></param>
        public RabbitService(string fileNamePath, string passphrase, string salt, ILoggerFactory loggerFactory = null, Func<PublishReceipt, ValueTask> processReceiptAsync = null)
            : this(
                  ConfigReader
                    .ConfigFileReadAsync(fileNamePath)
                    .GetAwaiter()
                    .GetResult(),
                  passphrase,
                  salt,
                  loggerFactory,
                  processReceiptAsync)
        { }

        /// <summary>
        /// Builds out a RabbitService with instantiated dependencies based on config settings.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="loggerFactory"></param>
        public RabbitService(Config config, string passphrase, string salt, ILoggerFactory loggerFactory = null, Func<PublishReceipt, ValueTask> processReceiptAsync = null)
        {
            LogHelper.LoggerFactory = loggerFactory;

            Config = config;
            ChannelPool = new ChannelPool(Config);

            if (!string.IsNullOrWhiteSpace(passphrase))
            {
                _hashKey = ArgonHash
                    .GetHashKeyAsync(passphrase, salt, Constants.EncryptionKeySize)
                    .GetAwaiter().GetResult();
            }

            Publisher = new Publisher(ChannelPool, _hashKey);
            Topologer = new Topologer(ChannelPool);

            BuildConsumers();

            StartAsync(passphrase, salt).GetAwaiter().GetResult();
        }

        private async Task StartAsync(string passphrase, string salt, Func<PublishReceipt, ValueTask> processReceiptAsync = null)
        {
            await _serviceLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await Publisher
                    .StartAutoPublishAsync(processReceiptAsync)
                    .ConfigureAwait(false);

                await BuildConsumerTopology()
                    .ConfigureAwait(false);
            }
            finally
            { _serviceLock.Release(); }
        }

        public async ValueTask ShutdownAsync(bool immediately)
        {
            await _serviceLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await Publisher
                    .StopAutoPublishAsync(immediately)
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
                // Apply the global consumer settings and global consumer pipeline settings
                // on top of (overriding) individual consumer settings. Opt out by not setting
                // the global settings field.
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
                    ConsumerPipelineNameToConsumerSetting.TryAdd(consumerSetting.Value.ConsumerPipelineSettings.ConsumerPipelineName, consumerSetting.Value);
                }
                Consumers.TryAdd(consumerSetting.Value.ConsumerName, new Consumer(ChannelPool, consumerSetting.Value, _hashKey));
            }
        }

        private async Task BuildConsumerTopology()
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

        public IConsumer<ReceivedData> GetConsumerByPipelineName(string consumerPipelineName)
        {
            if (!ConsumerPipelineNameToConsumerSetting.ContainsKey(consumerPipelineName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerPipelineSettingsMessage, consumerPipelineName));
            if (!Consumers.ContainsKey(ConsumerPipelineNameToConsumerSetting[consumerPipelineName].ConsumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ExceptionMessages.NoConsumerSettingsMessage, ConsumerPipelineNameToConsumerSetting[consumerPipelineName].ConsumerName));
            return Consumers[ConsumerPipelineNameToConsumerSetting[consumerPipelineName].ConsumerName];
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
