using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Configs;
using CookedRabbit.Core.Models;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utf8Json;

namespace CookedRabbit.Core.Consumer
{
    public class Consumer
    {
        public Config Config { get; }

        private ChannelPool ChannelPool { get; }
        private Channel<RabbitMessage> RabbitMessageBuffer;

        private ChannelHost ConsumingChannelHost { get; set; }
        private Task ConsumingTask { get; set; }

        public string ConsumerName { get; }
        public string QueueName { get; }
        public bool NoLocal { get; }
        public bool Exclusive { get; }
        public ushort QosPrefetchCount { get; }

        public bool Shutdown { get; private set; }

        public Consumer(Config config, string consumerName)
        {
            Config = config;
            if (!Config.ConsumerSettings.ContainsKey(consumerName)) throw new ArgumentException($"Consumer {consumerName} not found in ConsumerSettings dictionary.");
            var conSettings = Config.ConsumerSettings[consumerName];

            ChannelPool = new ChannelPool(Config);

            RabbitMessageBuffer = Channel.CreateBounded<RabbitMessage>(
                new BoundedChannelOptions(conSettings.RabbitMessageBufferSize)
                {
                    FullMode = conSettings.BehaviorWhenFull
                });

            ConsumerName = conSettings.ConsumerName;
            QueueName = conSettings.QueueName;
            NoLocal = conSettings.NoLocal;
            Exclusive = conSettings.Exclusive;
            QosPrefetchCount = conSettings.QosPrefetchCount;
        }

        public Consumer(ChannelPool channelPool)
        {
            Config = channelPool.Config;
            ChannelPool = channelPool;
        }

        public async Task StartConsumeAsync(bool useTransientChannel)
        {
            if (ConsumingTask == null)
            {
                await RabbitMessageBuffer
                    .Writer
                    .WaitToWriteAsync()
                    .ConfigureAwait(false);

                ConsumingTask = ConsumeAsync(useTransientChannel);
            }
        }

        public async Task ConsumeAsync(bool autoAck, bool useTransientChannel = true)
        {
            // Get Channel
            // Create Consumer
            // Start Consumer
            // Retry at Get Channel
            // Retry

            async Task GetChannelHostAsync()
            {
                try
                {
                    if (useTransientChannel)
                    {
                        ConsumingChannelHost = await ChannelPool
                            .GetTransientChannelAsync(!autoAck)
                            .ConfigureAwait(false);
                    }
                    else if (autoAck)
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

            async Task ConsumeLoopAsync()
            {
                while (true)
                {
                    if (Shutdown) { return; }

                    await GetChannelHostAsync().ConfigureAwait(false);
                    if (ConsumingChannelHost == null) { continue; }

                    if (Config.FactorySettings.EnableDispatchConsumersAsync)
                    {
                        var consumer = CreateAsyncConsumer(!autoAck);
                        if (consumer == null) { continue; }

                        ConsumingChannelHost.Channel.BasicConsume(QueueName, autoAck, ConsumerName, NoLocal, Exclusive, null, consumer);
                    }
                    else
                    {
                        var consumer = CreateConsumer(!autoAck);
                        if (consumer == null) { continue; }

                        ConsumingChannelHost.Channel.BasicConsume(QueueName, autoAck, ConsumerName, NoLocal, Exclusive, null, consumer);
                    }
                }
            };
            await ConsumeLoopAsync().ConfigureAwait(false);
        }

        private EventingBasicConsumer CreateConsumer(bool ackable)
        {
            EventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.Channel.BasicQos(0, QosPrefetchCount, false);
                consumer = new EventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandler;
                async void ReceiveHandler(object o, BasicDeliverEventArgs bdea)
                {
                    var rabbitMessage = new RabbitMessage(ConsumingChannelHost.Channel, bdea, ackable);

                    if (await RabbitMessageBuffer
                        .Writer
                        .WaitToWriteAsync()
                        .ConfigureAwait(false))
                    {
                        await RabbitMessageBuffer.Writer.WriteAsync(rabbitMessage);
                    }
                }
            }
            catch {  }

            return consumer;
        }

        private AsyncEventingBasicConsumer CreateAsyncConsumer(bool ackable)
        {
            AsyncEventingBasicConsumer consumer = null;

            try
            {
                ConsumingChannelHost.Channel.BasicQos(0, QosPrefetchCount, false);
                consumer = new AsyncEventingBasicConsumer(ConsumingChannelHost.Channel);

                consumer.Received += ReceiveHandlerAsync;
                async Task ReceiveHandlerAsync(object o, BasicDeliverEventArgs bdea)
                {
                    var rabbitMessage = new RabbitMessage(ConsumingChannelHost.Channel, bdea, ackable);

                    if (await RabbitMessageBuffer.Writer.WaitToWriteAsync().ConfigureAwait(false))
                    {
                        await RabbitMessageBuffer
                            .Writer
                            .WriteAsync(rabbitMessage);
                    }
                }
            }
            catch { }

            return consumer;
        }

        public async Task StopConsumingAsync(bool immediate)
        {

        }

        /// <summary>
        /// Simple retrieve message (byte[]) from queue. Null if nothing was available or on error. Exception possibly on retrieving Channel.
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
        /// Simple retrieve message (byte[]) from queue and convert to <see cref="{T}" /> efficiently. Default (assumed null) if nothing was available (or on transmission error). Exception possibly on retrieving Channel.
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
    }
}
