using System.Threading.Tasks;
using CookedRabbit.Core.Configs;
using CookedRabbit.Core.Pools;
using RabbitMQ.Client;
using Utf8Json;

namespace CookedRabbit.Core.Consumer
{
    public class Consumer
    {
        public Config Config { get; }

        private ChannelPool ChannelPool { get; }

        public Consumer(Config config)
        {
            Config = config;
            ChannelPool = new ChannelPool(Config);
        }

        public Consumer(ChannelPool channelPool)
        {
            Config = channelPool.Config;
            ChannelPool = channelPool;
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

        public async Task ConsumeAsync(bool useTransientChannel)
        {

        }

        public async Task StopAsync(bool immediate)
        {

        }
    }
}
