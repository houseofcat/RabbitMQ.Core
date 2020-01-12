using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utf8Json;

namespace CookedRabbit.Core
{
    public class LetterConsumer
    {
        private Config Config { get; }

        public ChannelPool ChannelPool { get; }
        private Channel<ReceivedLetter> LetterBuffer { get; set; }

        private ChannelHost ConsumingChannelHost { get; set; }

        public string ConsumerName { get; }
        public string QueueName { get; }
        public bool NoLocal { get; }
        public bool Exclusive { get; }
        public ushort QosPrefetchCount { get; }

        public bool Consuming { get; private set; }
        public bool Shutdown { get; private set; }

        private bool AutoAck { get; set; }
        private bool UseTransientChannel { get; set; }
        private readonly SemaphoreSlim conLock = new SemaphoreSlim(1, 1);
        private byte[] HashKey { get; set; }

        public LetterConsumer(Config config, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(config, nameof(config));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = config;
            ChannelPool = new ChannelPool(Config);
            HashKey = hashKey;

            var conSettings = Config.GetConsumerSettings(consumerName);
            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        public LetterConsumer(ChannelPool channelPool, string consumerName, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

            var conSettings = Config.GetConsumerSettings(consumerName);
            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        public LetterConsumer(ChannelPool channelPool, ConsumerOptions consumerSettings, byte[] hashKey = null)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNull(consumerSettings, nameof(consumerSettings));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            HashKey = hashKey;

            ConsumerName = consumerSettings.ConsumerName;
            QueueName = consumerSettings.QueueName;
            NoLocal = consumerSettings.NoLocal;
            Exclusive = consumerSettings.Exclusive;
            QosPrefetchCount = consumerSettings.QosPrefetchCount;
        }

        public async Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true)
        {
            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (!Consuming)
                {
                    AutoAck = autoAck;
                    UseTransientChannel = useTransientChannel;

                    var conSettings = Config.GetConsumerSettings(ConsumerName);

                    LetterBuffer = Channel.CreateBounded<ReceivedLetter>(
                        new BoundedChannelOptions(conSettings.MessageBufferSize)
                        {
                            FullMode = conSettings.BehaviorWhenFull
                        });

                    await LetterBuffer
                        .Writer
                        .WaitToWriteAsync()
                        .ConfigureAwait(false);

                    bool success;
                    do { success = await StartConsumingAsync().ConfigureAwait(false); }
                    while (!success);

                    Consuming = true;
                    Shutdown = false;
                }
            }
            finally { conLock.Release(); }
        }

        public async Task StopConsumingAsync(bool immediate = false)
        {
            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                LetterBuffer.Writer.Complete();

                if (!immediate)
                {
                    await LetterBuffer
                        .Reader
                        .Completion
                        .ConfigureAwait(false);
                }

                Shutdown = true;
            }
            finally { conLock.Release(); }
        }

        private async Task<bool> StartConsumingAsync()
        {
            await SetChannelHostAsync()
                .ConfigureAwait(false);

            if (ConsumingChannelHost == null) { return false; }

            if (Config.FactorySettings.EnableDispatchConsumersAsync)
            {
                var consumer = CreateAsyncConsumer();
                if (consumer == null) { return false; }

                ConsumingChannelHost
                    .Channel
                    .BasicConsume(QueueName, AutoAck, ConsumerName, NoLocal, Exclusive, null, consumer);
            }
            else
            {
                var consumer = CreateConsumer();
                if (consumer == null) { return false; }

                ConsumingChannelHost
                    .Channel
                    .BasicConsume(QueueName, AutoAck, ConsumerName, NoLocal, Exclusive, null, consumer);
            }

            return true;
        }

        private async Task SetChannelHostAsync()
        {
            try
            {
                if (UseTransientChannel)
                {
                    ConsumingChannelHost = await ChannelPool
                        .GetTransientChannelAsync(!AutoAck)
                        .ConfigureAwait(false);
                }
                else if (AutoAck)
                {
                    ConsumingChannelHost = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);
                }
                else
                {
                    ConsumingChannelHost = await ChannelPool
                        .GetAckChannelAsync()
                        .ConfigureAwait(false);
                }
            }
            catch { ConsumingChannelHost = null; }
        }

        private EventingBasicConsumer CreateConsumer()
        {
            EventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.Channel.BasicQos(0, QosPrefetchCount, false);
                consumer = new EventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandler;
                consumer.Shutdown += ConsumerShutdown;
            }
            catch { }

            return consumer;
        }

#pragma warning disable RCS1163 // Unused parameter.
        private async void ReceiveHandler(object o, BasicDeliverEventArgs bdea)
#pragma warning restore RCS1163 // Unused parameter.
        {
            var receivedLetter = new ReceivedLetter(ConsumingChannelHost.Channel, bdea, !AutoAck);

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

            if (await LetterBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false))
            {
                await LetterBuffer
                    .Writer
                    .WriteAsync(receivedLetter);
            }
        }

        private async void ConsumerShutdown(object sender, ShutdownEventArgs e)
        {
            bool success;
            do { success = await StartConsumingAsync().ConfigureAwait(false); }
            while (!success);
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer()
        {
            AsyncEventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.Channel.BasicQos(0, QosPrefetchCount, false);
                consumer = new AsyncEventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandlerAsync;
                consumer.Shutdown += ConsumerShutdownAsync;
            }
            catch { }

            return consumer;
        }

#pragma warning disable RCS1163 // Unused parameter.
        private async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
#pragma warning restore RCS1163 // Unused parameter.
        {
            var receivedLetter = new ReceivedLetter(ConsumingChannelHost.Channel, bdea, !AutoAck);

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

            if (await LetterBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                await LetterBuffer
                    .Writer
                    .WriteAsync(receivedLetter);
            }
        }

        private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
        {
            bool success;
            do { success = await StartConsumingAsync().ConfigureAwait(false); }
            while (!success);
        }

        /// <summary>
        /// Simple retrieve message (byte[]) from queue. Null if nothing was available or on error. Exception possible when retrieving Channel.
        /// <para>AutoAcks message.</para>
        /// </summary>
        /// <param name="queueName"></param>
        public async Task<byte[]> GetAsync(string queueName)
        {
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            BasicGetResult result = null;
            var error = false;
            try
            {
                result = chanHost
                    .Channel
                    .BasicGet(queueName, true);
            }
            catch { error = true; }

            await ChannelPool
                .ReturnChannelAsync(chanHost, error)
                .ConfigureAwait(false);

            return result?.Body;
        }

        /// <summary>
        /// Simple retrieve message (byte[]) from queue and convert to <see cref="{T}" /> efficiently. Default (assumed null) if nothing was available (or on transmission error). Exception possible when retrieving Channel.
        /// <para>AutoAcks message.</para>
        /// </summary>
        /// <param name="queueName"></param>
        public async Task<T> GetAsync<T>(string queueName)
        {
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            BasicGetResult result = null;
            var error = false;
            try
            {
                result = chanHost
                    .Channel
                    .BasicGet(queueName, true);
            }
            catch { error = true; }

            await ChannelPool
                .ReturnChannelAsync(chanHost, error)
                .ConfigureAwait(false);

            return result != null ? JsonSerializer.Deserialize<T>(result.Body) : default;
        }

        public ChannelReader<ReceivedLetter> GetConsumerLetterBuffer() => LetterBuffer.Reader;

        public async ValueTask<ReceivedLetter> ReadLetterAsync()
        {
            if (!await LetterBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            return await LetterBuffer
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReceivedLetter>> ReadLettersUntilEmptyAsync()
        {
            if (!await LetterBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            var list = new List<ReceivedLetter>();
            await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (LetterBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                list.Add(message);
            }

            return list;
        }

#if CORE3
        public async IAsyncEnumerable<ReceivedLetter> StreamOutLettersUntilEmptyAsync()
        {
            if (!await LetterBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await LetterBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (LetterBuffer.Reader.TryRead(out var message))
            {
                if (message == null) { break; }
                yield return message;
            }
        }

        public async IAsyncEnumerable<ReceivedLetter> StreamOutLettersUntilClosedAsync()
        {
            if (!await LetterBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await foreach (var receipt in LetterBuffer.Reader.ReadAllAsync())
            {
                yield return receipt;
            }
        }
#endif
    }
}
