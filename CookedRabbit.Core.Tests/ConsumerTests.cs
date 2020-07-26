using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Service;
using CookedRabbit.Core.Utils;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class ConsumerTests
    {
        private readonly ITestOutputHelper output;
        private readonly Config config;
        private readonly IChannelPool channelPool;
        private readonly Topologer topologer;
        private readonly IRabbitService rabbitService;

        public ConsumerTests(ITestOutputHelper output)
        {
            this.output = output;
            config = ConfigReader.ConfigFileReadAsync("TestConfig.json").GetAwaiter().GetResult();

            channelPool = new ChannelPool(config);
            channelPool.InitializeAsync().GetAwaiter().GetResult();
            topologer = new Topologer(config);
            rabbitService = new RabbitService("Config.json", null, null, null, null);
        }

        [Fact]
        public async Task CreateConsumer()
        {
            var config = await ConfigReader.ConfigFileReadAsync("TestConfig.json");
            Assert.NotNull(config);

            var con = new Consumer(config, "TestMessageConsumer");
            Assert.NotNull(con);
        }

        [Fact]
        public async Task CreateConsumerAndInitializeChannelPool()
        {
            var config = await ConfigReader.ConfigFileReadAsync("TestConfig.json");
            Assert.NotNull(config);

            var con = new Consumer(config, "TestMessageConsumer");
            Assert.NotNull(con);

            await con.ChannelPool.InitializeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateConsumerAndStart()
        {
            await topologer.CreateQueueAsync("TestConsumerQueue").ConfigureAwait(false);
            var con = new Consumer(channelPool, "TestMessageConsumer");
            await con.StartConsumerAsync(true, true).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateConsumerStartAndStop()
        {
            await topologer.CreateQueueAsync("TestConsumerQueue").ConfigureAwait(false);
            var con = new Consumer(channelPool, "TestMessageConsumer");

            await con.StartConsumerAsync(true, true).ConfigureAwait(false);
            await con.StopConsumerAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task StartAndStopTesting()
        {
            var consumer = rabbitService.GetConsumer("ConsumerFromConfig");

            await consumer.StartConsumerAsync(true, true).ConfigureAwait(false);
            await consumer.StopConsumerAsync().ConfigureAwait(false);
            await consumer.StartConsumerAsync(true, true).ConfigureAwait(false);
        }
    }
}
