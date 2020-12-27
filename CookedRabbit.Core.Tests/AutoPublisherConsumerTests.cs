using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static CookedRabbit.Core.Utils.RandomData;

namespace CookedRabbit.Core.Tests
{
    public class AutoPublisherConsumerTests
    {
        private readonly ITestOutputHelper output;
        private readonly Config config;
        private readonly Topologer topologer;
        private readonly AutoPublisher autoPublisher;
        private readonly Consumer consumer;

        public AutoPublisherConsumerTests(ITestOutputHelper output)
        {
            this.output = output;
            config = ConfigReader.ConfigFileReadAsync("Config.json").GetAwaiter().GetResult();

            var channelPool = new ChannelPool(config);
            topologer = new Topologer(channelPool);
            topologer.InitializeAsync().GetAwaiter().GetResult();

            autoPublisher = new AutoPublisher(channelPool);
            consumer = new Consumer(channelPool, "ConsumerFromConfig");
        }

        [Fact]
        public async Task AutoPublishAndConsume()
        {
            await autoPublisher.StartAsync().ConfigureAwait(false);

            const ulong count = 10000;

            var processReceiptsTask = ProcessReceiptsAsync(autoPublisher, count);
            var publishLettersTask = PublishLettersAsync(autoPublisher, count);
            var consumeMessagesTask = ConsumeMessagesAsync(consumer, count);

            while (!publishLettersTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            while (!processReceiptsTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            await autoPublisher.StopAsync().ConfigureAwait(false);

            while (!consumeMessagesTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            Assert.True(publishLettersTask.IsCompletedSuccessfully);
            Assert.True(processReceiptsTask.IsCompletedSuccessfully);
            Assert.True(consumeMessagesTask.IsCompletedSuccessfully);
            Assert.False(processReceiptsTask.Result);
            Assert.False(consumeMessagesTask.Result);

            await topologer.DeleteQueueAsync("TestAutoPublisherConsumerQueue").ConfigureAwait(false);
        }

        private async Task PublishLettersAsync(AutoPublisher apub, ulong count)
        {
            var sw = Stopwatch.StartNew();
            for (ulong i = 0; i < count; i++)
            {
                var letter = CreateSimpleRandomLetter("TestAutoPublisherConsumerQueue");
                letter.LetterId = i;

                await apub.QueueLetterAsync(letter).ConfigureAwait(false);
            }
            sw.Stop();

            output.WriteLine($"Finished queueing all letters in {sw.ElapsedMilliseconds} ms.");
        }

        private async Task<bool> ProcessReceiptsAsync(AutoPublisher apub, ulong count)
        {
            await Task.Yield();

            var buffer = apub.GetReceiptBufferReader();
            var receiptCount = 0ul;
            var error = false;

            var sw = Stopwatch.StartNew();
            while (receiptCount < count)
            {
                if (buffer.TryRead(out var receipt))
                {
                    receiptCount++;
                    if (receipt.IsError)
                    { error = true; break; }
                }
            }
            sw.Stop();

            output.WriteLine($"Finished getting receipts.\r\nReceiptCount: {receiptCount} in {sw.ElapsedMilliseconds} ms.\r\nErrorStatus: {error}");

            return error;
        }

        private async Task<bool> ConsumeMessagesAsync(Consumer consumer, ulong count)
        {
            var messageCount = 0ul;
            var error = false;

            await consumer
                .StartConsumerAsync(true, true)
                .ConfigureAwait(false);

            var sw = Stopwatch.StartNew();
            while (messageCount < count)
            {
                try
                {
                    var message = await consumer.ReadAsync().ConfigureAwait(false);
                    messageCount++;
                }
                catch
                { error = true; break; }
            }
            sw.Stop();

            output.WriteLine($"Finished consuming messages.\r\nMessageCount: {messageCount} in {sw.ElapsedMilliseconds} ms.\r\nErrorStatus: {error}");
            return error;
        }
    }
}
