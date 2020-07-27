using CookedRabbit.Core.Pools;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class ChannelPoolTests
    {
        private readonly ITestOutputHelper output;

        public ChannelPoolTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CreateChannelPoolWithLocalHost()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var chanPool = new ChannelPool(config);

            Assert.NotNull(chanPool);
        }

        [Fact]
        public async Task InitializeChannelPoolAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var chanPool = new ChannelPool(config);

            Assert.NotNull(chanPool);
            Assert.True(chanPool.CurrentChannelId > 0);
        }

        [Fact]
        public async Task OverLoopThroughChannelPoolAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            config.PoolSettings.MaxConnections = 5;
            config.PoolSettings.MaxChannels = 25;
            var successCount = 0;
            const int loopCount = 100_000;
            var chanPool = new ChannelPool(config);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < loopCount; i++)
            {
                var channel = await chanPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);

                if (channel != null)
                {
                    successCount++;
                    await chanPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            }

            for (int i = 0; i < loopCount; i++)
            {
                var channel = await chanPool
                    .GetAckChannelAsync()
                    .ConfigureAwait(false);

                if (channel != null)
                {
                    successCount++;
                    await chanPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            }

            sw.Stop();
            output.WriteLine($"OverLoop Iteration Time: {sw.ElapsedMilliseconds} ms");

            Assert.True(successCount == 2 * loopCount);
        }
    }
}
