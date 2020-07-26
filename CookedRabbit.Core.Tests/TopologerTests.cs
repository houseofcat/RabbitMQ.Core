using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CookedRabbit.Core.Tests
{
    public class TopologerTests
    {
        [Fact]
        public void CreateTopologer()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);

            Assert.NotNull(top);
        }

        [Fact]
        public async Task CreateTopologerAndInitializeChannelPool()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);

            Assert.NotNull(top);
        }

        [Fact]
        public async Task CreateTopologerWithChannelPool()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var chanPool = new ChannelPool(config);
            var top = new Topologer(chanPool);

            Assert.NotNull(top);
        }

        [Fact]
        public async Task CreateQueueWithoutInitializeAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);

            await Assert
                .ThrowsAsync<InvalidOperationException>(() => top.CreateQueueAsync("TestQueue", false, false, false, null))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateQueueAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);
            var error = await top.CreateQueueAsync("TestQueueTest", false, false, false, null).ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndDeleteQueueAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);
            var error = await top.CreateQueueAsync("TestQueueTest", false, false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteQueueAsync("TestQueueTest", false, false).ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateExchangeAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndDeleteExchangeAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchangeTest").ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndBindQueueAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.CreateQueueAsync("TestQueueTest", false, false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.BindQueueToExchangeAsync("TestQueueTest", "TestExchangeTest", "TestRoutingKeyTest", null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchangeTest").ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteQueueAsync("TestQueueTest").ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateAndBindExchangeAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.CreateExchangeAsync("TestExchange2Test", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.BindExchangeToExchangeAsync("TestExchange2Test", "TestExchangeTest", "TestRoutingKeyTest", null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchangeTest").ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchange2Test").ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateBindAndUnbindExchangeAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var top = new Topologer(config);
            var error = await top.CreateExchangeAsync("TestExchangeTest", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.CreateExchangeAsync("TestExchange2Test", "direct", false, false, null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.BindExchangeToExchangeAsync("TestExchange2Test", "TestExchangeTest", "TestRoutingKeyTest", null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.UnbindExchangeFromExchangeAsync("TestExchange2Test", "TestExchangeTest", "TestRoutingKeyTest", null).ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchangeTest").ConfigureAwait(false);
            Assert.False(error);

            error = await top.DeleteExchangeAsync("TestExchange2Test").ConfigureAwait(false);
            Assert.False(error);
        }

        [Fact]
        public async Task CreateTopologyFromFileAsync()
        {
            var config = await ConfigReader.ConfigFileReadAsync("TestConfig.json");
            var top = new Topologer(config);
            await top
                .CreateTopologyFromFileAsync("TestTopologyConfig.json")
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateTopologyFromPartialFileAsync()
        {
            var config = await ConfigReader.ConfigFileReadAsync("TestConfig.json");
            var top = new Topologer(config);
            await top
                .CreateTopologyFromFileAsync("TestPartialTopologyConfig.json")
                .ConfigureAwait(false);
        }
    }
}
