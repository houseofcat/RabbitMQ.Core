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
            config = Utils.ConfigReader.ConfigFileRead("TestConfig.json");

            topologer = new Topologer(config);
            topologer.ChannelPool.InitializeAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public void CreateConsumer()
        {
            var config = Utils.ConfigReader.ConfigFileRead("TestConfig.json");
            Assert.NotNull(config);

            var con = new MessageConsumer(config, "TestMessageConsumer");
            Assert.NotNull(con);
        }

        [Fact]
        public async Task CreateConsumerAndInitializeChannelPool()
        {
            var config = Utils.ConfigReader.ConfigFileRead("TestConfig.json");
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
