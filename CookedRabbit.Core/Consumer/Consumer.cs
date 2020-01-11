using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class Consumer
    {
        public Config Config { get; }

        public ChannelPool ChannelPool { get; }
        private Channel<RabbitMessage> RabbitMessageBuffer { get; set; }

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

        public Consumer(Config config, string consumerName)
        {
            Guard.AgainstNull(config, nameof(config));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = config;
            ChannelPool = new ChannelPool(Config);

            ConsumerName = consumerName;
            var conSettings = GetConsumerSettings(consumerName);

            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        public Consumer(ChannelPool channelPool, string consumerName)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));
            Guard.AgainstNullOrEmpty(consumerName, nameof(consumerName));

            Config = channelPool.Config;
            ChannelPool = channelPool;

            ConsumerName = consumerName;
            var conSettings = GetConsumerSettings(consumerName);

            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        private ConsumerOptions GetConsumerSettings(string consumerName)
        {
            if (!Config.ConsumerSettings.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Strings.NoConsumerSettingsMessage, consumerName));
            return Config.ConsumerSettings[consumerName];
        }

        public async Task StartConsumerAsync(bool autoAck = false, bool useTransientChannel = true)
        {
            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

            if (!Consuming)
            {
                AutoAck = autoAck;
                UseTransientChannel = useTransientChannel;
                var conSettings = GetConsumerSettings(ConsumerName);

                RabbitMessageBuffer = Channel.CreateBounded<RabbitMessage>(
                new BoundedChannelOptions(conSettings.RabbitMessageBufferSize)
                {
                    FullMode = conSettings.BehaviorWhenFull
                });

                await RabbitMessageBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false);

                bool success;
                do { success = await StartConsumingAsync().ConfigureAwait(false); }
                while (!success);

                Consuming = true;
                Shutdown = false;
            }

            conLock.Release();
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
            var rabbitMessage = new RabbitMessage(ConsumingChannelHost.Channel, bdea, !AutoAck);

            if (await RabbitMessageBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false))
            {
                await RabbitMessageBuffer
                    .Writer
                    .WriteAsync(rabbitMessage);
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
        public async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
#pragma warning restore RCS1163 // Unused parameter.
        {
            var rabbitMessage = new RabbitMessage(ConsumingChannelHost.Channel, bdea, !AutoAck);

            if (await RabbitMessageBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                await RabbitMessageBuffer
                    .Writer
                    .WriteAsync(rabbitMessage);
            }
        }

        private async Task ConsumerShutdownAsync(object sender, ShutdownEventArgs e)
        {
            bool success;
            do { success = await StartConsumingAsync().ConfigureAwait(false); }
            while (!success);
        }

        public async Task StopConsumingAsync(bool immediate = false)
        {
            await conLock
                .WaitAsync()
                .ConfigureAwait(false);

            RabbitMessageBuffer.Writer.Complete();

            if (!immediate)
            {
                await RabbitMessageBuffer
                    .Reader
                    .Completion
                    .ConfigureAwait(false);
            }

            Shutdown = true;

            conLock.Release();
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

        public ChannelReader<RabbitMessage> GetConsumerRabbitMessageBuffer() => RabbitMessageBuffer.Reader;

        public async ValueTask<RabbitMessage> ReadRabbitMessageAsync()
        {
            if (!await RabbitMessageBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            return await RabbitMessageBuffer
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<RabbitMessage>> ReadRabbitMessagesUntilEmptyAsync()
        {
            if (!await RabbitMessageBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            var list = new List<RabbitMessage>();
            await RabbitMessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (RabbitMessageBuffer.Reader.TryRead(out var message))
            {
                list.Add(message);
            }

            return list;
        }

#if CORE3
        public async IAsyncEnumerable<RabbitMessage> StreamOutRabbitMessagesUntilEmptyAsync()
        {
            if (!await RabbitMessageBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await RabbitMessageBuffer.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (RabbitMessageBuffer.Reader.TryRead(out var message))
            {
                yield return message;
            }
        }

        public async IAsyncEnumerable<RabbitMessage> StreamOutRabbitMessagesUntilClosedAsync()
        {
            if (!await RabbitMessageBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await foreach (var receipt in RabbitMessageBuffer.Reader.ReadAllAsync())
            {
                yield return receipt;
            }
        }
#endif
    }
}
