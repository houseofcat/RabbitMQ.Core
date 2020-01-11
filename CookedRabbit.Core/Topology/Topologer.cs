using System.Collections.Generic;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;

namespace CookedRabbit.Core
{
    public class Topologer
    {
        public Config Config { get; }
        public ChannelPool ChannelPool { get; }

        public Topologer(Config config)
        {
            Guard.AgainstNull(config, nameof(config));

            Config = config;
            ChannelPool = new ChannelPool(Config);
        }

        public Topologer(ChannelPool channelPool)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));

            Config = channelPool.Config;
            ChannelPool = channelPool;
        }

        /// <summary>
        /// Create a queue asynchronously.
        /// <para>Returns success or failure.</para>
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="durable"></param>
        /// <param name="exclusive"></param>
        /// <param name="autoDelete"></param>
        /// <param name="args"></param>
        /// <returns>A bool indicating failure.</returns>
        public async Task<bool> CreateQueueAsync(
            string queueName,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            IDictionary<string, object> args = null)
        {
            Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

            var error = false;
            var chanHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                chanHost.Channel.QueueDeclare(
                    queue: queueName,
                    durable: durable,
                    exclusive: exclusive,
                    autoDelete: autoDelete,
                    arguments: args);
            }
            catch { error = true; }
            finally { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

            return error;
        }

        /// <summary>
        /// Delete a queue asynchronously.
        /// <para>Returns success or failure.</para>
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="onlyIfUnused"></param>
        /// <param name="onlyIfEmpty"></param>
        /// <returns>A bool indicating failure.</returns>
        public async Task<bool> DeleteQueueAsync(
            string queueName,
            bool onlyIfUnused = false,
            bool onlyIfEmpty = false)
        {
            Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

            var error = false;
            var chanHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                chanHost.Channel.QueueDelete(
                    queue: queueName,
                    ifUnused: onlyIfUnused,
                    ifEmpty: onlyIfEmpty);
            }
            catch { error = true; }
            finally { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

            return error;
        }

        /// <summary>
        /// Bind a queue to exchange asynchronously.
        /// <para>Returns success or failure.</para>
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="args"></param>
        /// <returns>A bool indicating failure.</returns>
        public async Task<bool> BindQueueToExchangeAsync(
            string queueName,
            string exchangeName,
            string routingKey = "",
            IDictionary<string, object> args = null)
        {
            Guard.AgainstNullOrEmpty(exchangeName, nameof(exchangeName));
            Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

            var error = false;
            var chanHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                chanHost.Channel.QueueBind(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: routingKey,
                    arguments: args);
            }
            catch { error = true; }
            finally { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

            return error;
        }

        /// <summary>
        /// Unbind a queue from Exchange asynchronously.
        /// <para>Returns success or failure.</para>
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="args"></param>
        /// <returns>A bool indicating failure.</returns>
        public async Task<bool> UnbindQueueFromExchangeAsync(
            string queueName,
            string exchangeName,
            string routingKey = "",
            IDictionary<string, object> args = null)
        {
            Guard.AgainstNullOrEmpty(exchangeName, nameof(exchangeName));
            Guard.AgainstNullOrEmpty(queueName, nameof(queueName));

            var error = false;
            var chanHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                chanHost.Channel.QueueUnbind(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: routingKey,
                    arguments: args);
            }
            catch { error = true; }
            finally { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

            return error;
        }

        /// <summary>
        /// Create an Exchange asynchronously.
        /// <para>Returns success or failure.</para>
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="exchangeType"></param>
        /// <param name="durable"></param>
        /// <param name="autoDelete"></param>
        /// <param name="args"></param>
        /// <returns>A bool indicating failure.</returns>
        public async Task<bool> CreateExchangeAsync(
            string exchangeName,
            string exchangeType,
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object> args = null)
        {
            Guard.AgainstNullOrEmpty(exchangeName, nameof(exchangeName));

            var error = false;
            var chanHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                chanHost.Channel.ExchangeDeclare(
                    exchange: exchangeName,
                    type: exchangeType,
                    durable: durable,
                    autoDelete: autoDelete,
                    arguments: args);
            }
            catch { error = true; }
            finally { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

            return error;
        }

        /// <summary>
        /// Delete an Exchange asynchronously.
        /// <para>Returns success or failure.</para>
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="onlyIfUnused"></param>
        /// <returns>A bool indicating failure.</returns>
        public async Task<bool> DeleteExchangeAsync(string exchangeName, bool onlyIfUnused = false)
        {
            Guard.AgainstNullOrEmpty(exchangeName, nameof(exchangeName));

            var error = false;
            var chanHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                chanHost.Channel.ExchangeDelete(
                    exchange: exchangeName,
                    ifUnused: onlyIfUnused);
            }
            catch { error = true; }
            finally { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

            return error;
        }

        /// <summary>
        /// Bind an Exchange to another Exchange asynchronously.
        /// <para>Returns success or failure.</para>
        /// </summary>
        /// <param name="childExchangeName"></param>
        /// <param name="parentExchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="args"></param>
        /// <returns>A bool indicating failure.</returns>
        public async Task<bool> BindExchangeToExchangeAsync(
            string childExchangeName,
            string parentExchangeName,
            string routingKey = "",
            IDictionary<string, object> args = null)
        {
            Guard.AgainstNullOrEmpty(parentExchangeName, nameof(parentExchangeName));
            Guard.AgainstNullOrEmpty(childExchangeName, nameof(childExchangeName));

            var error = false;
            var chanHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                chanHost.Channel.ExchangeBind(
                    destination: childExchangeName,
                    source: parentExchangeName,
                    routingKey: routingKey,
                    arguments: args);
            }
            catch { error = true; }
            finally { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

            return error;
        }

        /// <summary>
        /// Unbind an Exchange from another Exchange asynchronously.
        /// <para>Returns success or failure.</para>
        /// </summary>
        /// <param name="childExchangeName"></param>
        /// <param name="parentExchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="args"></param>
        /// <returns>A bool indicating failure.</returns>
        public async Task<bool> UnbindExchangeFromExchangeAsync(
            string childExchangeName,
            string parentExchangeName,
            string routingKey = "",
            IDictionary<string, object> args = null)
        {
            Guard.AgainstNullOrEmpty(parentExchangeName, nameof(parentExchangeName));
            Guard.AgainstNullOrEmpty(childExchangeName, nameof(childExchangeName));

            var error = false;
            var chanHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                chanHost.Channel.ExchangeUnbind(
                    destination: childExchangeName,
                    source: parentExchangeName,
                    routingKey: routingKey,
                    arguments: args);
            }
            catch { error = true; }
            finally { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }

            return error;
        }
    }
}
