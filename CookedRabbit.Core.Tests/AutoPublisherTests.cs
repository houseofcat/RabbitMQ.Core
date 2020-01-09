using CookedRabbit.Core.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class AutoPublisherTests
    {
        private readonly ITestOutputHelper output;
        private readonly Config config;
        private readonly Topologer topologer;

        public AutoPublisherTests(ITestOutputHelper output)
        {
            this.output = output;
            config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            topologer = new Topologer(config);
            topologer.ChannelPool.InitializeAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public void CreateAutoPublisher()
        {
            var apub = new AutoPublisher(topologer.ChannelPool);

            Assert.NotNull(apub);
        }

        [Fact]
        public async Task CreateAutoPublisherAndStart()
        {
            var apub = new AutoPublisher(topologer.ChannelPool);
            await apub.StartAsync().ConfigureAwait(false);

            Assert.NotNull(apub);
        }

        [Fact]
        public async Task CreateAutoPublisherAndPublish()
        {
            var apub = new AutoPublisher(topologer.ChannelPool);

            var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
            await apub.QueueLetterAsync(letter).ConfigureAwait(false);
        }

        [Fact]
        public void CreateAutoPublisherByConfig()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var apub = new AutoPublisher(config);

            Assert.NotNull(apub);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigAndStart()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var apub = new AutoPublisher(config);
            await apub.StartAsync().ConfigureAwait(false);

            Assert.NotNull(apub);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigAndPublish()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var apub = new AutoPublisher(config);
            await apub.StartAsync().ConfigureAwait(false);

            var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
            await apub.QueueLetterAsync(letter).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateAutoPublisherByConfigQueueAndConcurrentPublish()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var apub = new AutoPublisher(config);
            await apub.StartAsync().ConfigureAwait(false);
            var finished = false;
            const ulong count = 10000;

            await Task.Run(async () =>
            {
                for (ulong i = 0; i < count; i++)
                {
                    var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
                    letter.LetterId = i;

                    await apub.QueueLetterAsync(letter).ConfigureAwait(false);
                }

                finished = true;
            }).ConfigureAwait(false);

            while (!finished) { await Task.Delay(1).ConfigureAwait(false); }

            await apub.StopAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateAutoPublisherQueueConcurrentPublishAndProcessReceipts()
        {
            var apub = new AutoPublisher(topologer.ChannelPool);
            await apub.StartAsync().ConfigureAwait(false);
            const ulong count = 10000;

            var processReceiptsTask = ProcessReceiptsAsync(apub, count);
            var publishLettersTask = PublishLettersAsync(apub, count);

            while (!publishLettersTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            while (!processReceiptsTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            Assert.True(publishLettersTask.IsCompletedSuccessfully);
            Assert.True(processReceiptsTask.IsCompletedSuccessfully);

            Assert.False(processReceiptsTask.Result);
        }

        private async Task PublishLettersAsync(AutoPublisher apub, ulong count)
        {
            var sw = Stopwatch.StartNew();
            for (ulong i = 0; i < count; i++)
            {
                var letter = RandomData.CreateSimpleRandomLetter("AutoPublisherTestQueue");
                letter.LetterId = i;

                await apub.QueueLetterAsync(letter).ConfigureAwait(false);
            }
            sw.Stop();

            output.WriteLine($"Finished queueing all letters in {sw.ElapsedMilliseconds} ms.");

            await apub.StopAsync().ConfigureAwait(false);
        }

        private async Task<bool> ProcessReceiptsAsync(AutoPublisher apub, ulong count)
        {
            var buffer = apub.GetReceiptBufferReader();
            var receiptCount = 0ul;
            var error = false;

            var sw = Stopwatch.StartNew();
            while (receiptCount < count)
            {
                var receipt = await buffer.ReadAsync().ConfigureAwait(false);
                if (receipt.IsError)
                {
                    error = true;
                }

                receiptCount++;
            }
            sw.Stop();

            output.WriteLine($"Finished getting receipts on all published letters in {sw.ElapsedMilliseconds} ms.");

            return error;
        }
    }
}
