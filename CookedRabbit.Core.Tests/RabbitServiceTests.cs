using CookedRabbit.Core.Service;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class RabbitServiceTests
    {
        //private readonly ITestOutputHelper output;
        private readonly RabbitService rabbitService;

        public RabbitServiceTests(ITestOutputHelper output)
        {
            rabbitService = new RabbitService("TestConfig.json");

            rabbitService
                .InitializeAsync("passwordforencryption", "saltforencryption")
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void GetMessageConsumer()
        {
            var consumer = rabbitService.GetMessageConsumer("TestMessageConsumer");

            Assert.NotNull(consumer);
        }

        [Fact]
        public void GetLetterConsumer()
        {
            var consumer = rabbitService.GetMessageConsumer("TestMessageConsumer");

            Assert.NotNull(consumer);
        }

        [Fact]
        public async Task ProductionBug_CantFindConsumer_WhenStartingMessageConsumers()
        {
            var rabbitService = new RabbitService("TestConfig.json");
            await rabbitService
                .InitializeAsync("passwordforencryption", "saltforencryption")
                .ConfigureAwait(false);

            await rabbitService
                .Topologer
                .CreateTopologyFromFileAsync("TestTopologyConfig.json")
                .ConfigureAwait(false);

            var consumer = rabbitService.GetMessageConsumer("TestMessageConsumer");
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);
        }
    }
}
