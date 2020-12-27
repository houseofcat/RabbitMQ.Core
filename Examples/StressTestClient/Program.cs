﻿using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CookedRabbit.Core.StressAndStabilityConsole
{
    public static class Program
    {
        private static Config config;
        private static ChannelPool channelPool;
        private static Topologer topologer;

        private static Publisher apub1;
        private static Publisher apub2;
        private static Publisher apub3;
        private static Publisher apub4;

        private static Consumer con1;
        private static Consumer con2;
        private static Consumer con3;
        private static Consumer con4;

        // Per Publisher
        private const ulong MessageCount = 250_000;
        private const int MessageSize = 1_000;

        public static async Task Main()
        {
            LogHelper.LoggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            await Console.Out.WriteLineAsync("CookedRabbit.Core StressTest v1.00").ConfigureAwait(false);
            await Console.Out.WriteLineAsync("- StressTest setting everything up...").ConfigureAwait(false);

            var setupFailed = false;
            try
            { await SetupAsync().ConfigureAwait(false); }
            catch (Exception ex)
            {
                setupFailed = true;
                await Console.Out.WriteLineAsync($"- StressTest failed with exception {ex.Message}.").ConfigureAwait(false);
            }

            if (!setupFailed)
            {
                await Console.Out.WriteLineAsync("- StressTest starting!").ConfigureAwait(false);

                try
                {
                    await StartStressTestAsync()
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                { await Console.Out.WriteLineAsync($"- StressTest failed with exception {ex.Message}.").ConfigureAwait(false); }
            }

            await Console.Out.WriteLineAsync($"- Press any key to begin shutdown.").ConfigureAwait(false);
            Console.ReadKey();

            await ShutdownAsync();

            await Console.Out.WriteLineAsync($"- Press any key to close.").ConfigureAwait(false);
            Console.ReadKey();
        }

        private static async Task SetupAsync()
        {
            var sw = Stopwatch.StartNew();
            config = await ConfigReader.ConfigFileReadAsync("Config.json");

            channelPool = new ChannelPool(config);

            topologer = new Topologer(channelPool);

            apub1 = new Publisher(channelPool, new byte[] { });
            apub2 = new Publisher(channelPool, new byte[] { });
            apub3 = new Publisher(channelPool, new byte[] { });
            apub4 = new Publisher(channelPool, new byte[] { });

            await Console.Out.WriteLineAsync("- Creating stress test queues!").ConfigureAwait(false);

            foreach (var kvp in config.ConsumerSettings)
            {
                await topologer
                    .DeleteQueueAsync(kvp.Value.QueueName)
                    .ConfigureAwait(false);
            }

            foreach (var kvp in config.ConsumerSettings)
            {
                await topologer
                    .CreateQueueAsync(kvp.Value.QueueName, true)
                    .ConfigureAwait(false);
            }

            await apub1.StartAutoPublishAsync().ConfigureAwait(false);
            await apub2.StartAutoPublishAsync().ConfigureAwait(false);
            await apub3.StartAutoPublishAsync().ConfigureAwait(false);
            await apub4.StartAutoPublishAsync().ConfigureAwait(false);

            con1 = new Consumer(channelPool, "Consumer1");
            con2 = new Consumer(channelPool, "Consumer2");
            con3 = new Consumer(channelPool, "Consumer3");
            con4 = new Consumer(channelPool, "Consumer4");
            sw.Stop();

            await Console
                .Out
                .WriteLineAsync($"- Setup has finished in {sw.ElapsedMilliseconds} ms.")
                .ConfigureAwait(false);
        }

        private static async Task StartStressTestAsync()
        {
            var sw = Stopwatch.StartNew();
            var pubSubTask1 = StartPubSubTestAsync(apub1, con1);
            var pubSubTask2 = StartPubSubTestAsync(apub2, con2);
            var pubSubTask3 = StartPubSubTestAsync(apub3, con3);
            var pubSubTask4 = StartPubSubTestAsync(apub4, con4);

            await Task
                .WhenAll(pubSubTask1, pubSubTask2, pubSubTask3, pubSubTask4)
                .ConfigureAwait(false);

            await Console.Out.WriteLineAsync($"- All tests finished in {sw.ElapsedMilliseconds / 60_000.0} minutes!").ConfigureAwait(false);
        }

        private static async Task ShutdownAsync()
        {
            await channelPool.ShutdownAsync();
        }

        private static async Task StartPubSubTestAsync(Publisher autoPublisher, Consumer consumer)
        {
            var publishLettersTask = PublishLettersAsync(autoPublisher, consumer.Options.QueueName, MessageCount);
            var processReceiptsTask = ProcessReceiptsAsync(autoPublisher, MessageCount);
            var consumeMessagesTask = ConsumeMessagesAsync(consumer, MessageCount);

            while (!publishLettersTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            while (!processReceiptsTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            await autoPublisher.StopAutoPublishAsync().ConfigureAwait(false);

            while (!consumeMessagesTask.IsCompleted)
            { await Task.Delay(1).ConfigureAwait(false); }

            await consumer.StopConsumerAsync();
            await Console.Out.WriteLineAsync($"- Consumer ({consumer.Options.ConsumerName}) stopped.").ConfigureAwait(false);
        }

        private static async Task PublishLettersAsync(Publisher apub, string queueName, ulong count)
        {
            var sw = Stopwatch.StartNew();
            for (ulong i = 0; i < count; i++)
            {
                var letter = RandomData.CreateSimpleRandomLetter(queueName, MessageSize);
                letter.Envelope.RoutingOptions.DeliveryMode = 1;
                letter.LetterId = i;

                await apub.QueueLetterAsync(letter).ConfigureAwait(false);

                if (letter.LetterId % 10_000 == 0)
                {
                    await Console
                        .Out
                        .WriteLineAsync($"- QueueName ({queueName}) is publishing letter {letter.LetterId}")
                        .ConfigureAwait(false);
                }
            }
            sw.Stop();

            await Console
                .Out
                .WriteLineAsync($"- Finished queueing all letters in {sw.ElapsedMilliseconds / 60_000.0} minutes.")
                .ConfigureAwait(false);
        }

        private static async Task ProcessReceiptsAsync(Publisher apub, ulong count)
        {
            var buffer = apub.GetReceiptBufferReader();
            var receiptCount = 0ul;
            var errorCount = 0ul;

            var sw = Stopwatch.StartNew();
            while (receiptCount + errorCount < count)
            {
                try
                {
                    var receipt = await buffer.ReadAsync().ConfigureAwait(false);
                    if (receipt.IsError)
                    {
                        errorCount++;
                    }

                    //await Task.Delay(1).ConfigureAwait(false);
                }
                catch { errorCount++; break; }

                receiptCount++;
            }
            sw.Stop();

            await Console.Out.WriteLineAsync($"- Finished getting receipts.\r\nReceiptCount: {receiptCount} in {sw.ElapsedMilliseconds / 60_000.0} minutes.\r\nErrorCount: {errorCount}").ConfigureAwait(false);
        }

        private static async Task ConsumeMessagesAsync(Consumer consumer, ulong count)
        {
            var messageCount = 0ul;
            var errorCount = 0ul;

            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);

            var sw = Stopwatch.StartNew();
            try
            {
                while (await consumer.GetConsumerBuffer().WaitToReadAsync()) // TODO: Possible Infinite loop on lost messages.
                {
                    while (consumer.GetConsumerBuffer().TryRead(out var message))
                    {
                        if (message.Ackable)
                        { message.AckMessage(); }

                        messageCount++;
                    }

                    if (messageCount + errorCount >= count)
                    { break; }
                }
            }
            catch
            { errorCount++; }
            sw.Stop();

            await Console.Out.WriteLineAsync($"- Finished consuming messages.\r\nMessageCount: {messageCount} in {sw.ElapsedMilliseconds / 60_000.0} minutes.\r\nErrorCount: {errorCount}").ConfigureAwait(false);
        }
    }
}
