using CookedRabbit.Core.Service;
using System;
using System.Threading.Tasks;
using Utf8Json;

namespace CookedRabbit.Core.SimpleClient
{
    public static class Program
    {
        public class TestMessage
        {
            public string Message { get; set; }
        }

        public static async Task Main()
        {
            await RunSimpleClientWithEncryptionAsync()
                .ConfigureAwait(false);
        }

        private static async Task RunSimpleClientWithEncryptionAsync()
        {
            await Console.Out.WriteLineAsync("Starting SimpleClient w/ Encryption...").ConfigureAwait(false);

            var sentMessage = new TestMessage { Message = "Sensitive Message" };
            var letter = new Letter("", "TestRabbitServiceQueue", JsonSerializer.Serialize(sentMessage), new LetterMetadata());

            var rabbitService = new RabbitService("Config.json");
            await rabbitService
                .InitializeAsync("passwordforencryption", "saltforencryption")
                .ConfigureAwait(false);

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            // Queue the letter for delivery by the library.
            await rabbitService
                .AutoPublisher
                .QueueLetterAsync(letter);

            // Start Consumer
            var consumer = rabbitService.GetConsumer("ConsumerFromConfig");
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);

            // Get Message From Consumer
            var receivedLetter = await consumer
                .ReadLetterAsync()
                .ConfigureAwait(false);

            // Do work with message inside the receivedLetter
            var decodedLetter = JsonSerializer.Deserialize<TestMessage>(receivedLetter.Letter.Body);

            await Console.Out.WriteLineAsync($"Sent: {sentMessage.Message}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"Received: {decodedLetter.Message}").ConfigureAwait(false);

            // Acknowledge Message
            if (receivedLetter.Ackable)
            { receivedLetter.AckMessage(); }

            // Cleanup the queue
            await rabbitService
                .Topologer
                .DeleteQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }
    }
}
