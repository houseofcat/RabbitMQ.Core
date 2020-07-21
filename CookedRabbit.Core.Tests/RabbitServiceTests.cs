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
                .InitializeAsync(true, "passwordforencryption", "saltforencryption")
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void GetMessageConsumer()
        {
            var consumer = rabbitService.GetConsumer("TestMessageConsumer");

            Assert.NotNull(consumer);
        }

        [Fact]
        public void GetLetterConsumer()
        {
            var consumer = rabbitService.GetConsumer("TestMessageConsumer");

            Assert.NotNull(consumer);
        }

        [Fact]
        public async Task ProductionBug_CantFindConsumer_WhenStartingMessageConsumers()
        {
            var rabbitService = new RabbitService("TestConfig.json");
            await rabbitService
                .InitializeAsync(true, "passwordforencryption", "saltforencryption")
                .ConfigureAwait(false);

            await rabbitService
                .Topologer
                .CreateTopologyFromFileAsync("TestTopologyConfig.json")
                .ConfigureAwait(false);

            var consumer = rabbitService.GetConsumer("TestMessageConsumer");
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);
        }
    }
}
