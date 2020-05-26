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
        private readonly Topologer topologer;

        public ConsumerTests(ITestOutputHelper output)
        {
            this.output = output;
            config = ConfigReader.ConfigFileReadAsync("TestConfig.json").GetAwaiter().GetResult();

            topologer = new Topologer(config);
            topologer.ChannelPool.InitializeAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task CreateConsumer()
        {
            var config = await ConfigReader.ConfigFileReadAsync("TestConfig.json");
            Assert.NotNull(config);

            var con = new MessageConsumer(config, "TestMessageConsumer");
            Assert.NotNull(con);
        }

        [Fact]
        public async Task CreateConsumerAndInitializeChannelPool()
        {
            var config = await ConfigReader.ConfigFileReadAsync("TestConfig.json");
            Assert.NotNull(config);

            var con = new MessageConsumer(config, "TestMessageConsumer");
            Assert.NotNull(con);

            await con.ChannelPool.InitializeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateConsumerAndStart()
        {
            await topologer.CreateQueueAsync("TestConsumerQueue").ConfigureAwait(false);
            var con = new MessageConsumer(topologer.ChannelPool, "TestMessageConsumer");
            await con.StartConsumerAsync(true, true).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateConsumerStartAndStop()
        {
            await topologer.CreateQueueAsync("TestConsumerQueue").ConfigureAwait(false);
            var con = new MessageConsumer(topologer.ChannelPool, "TestMessageConsumer");

            await con.StartConsumerAsync(true, true).ConfigureAwait(false);
            await con.StopConsumerAsync().ConfigureAwait(false);
        }
    }
}
