using CookedRabbit.Core.Service;
using CookedRabbit.Core.WorkEngines;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CookedRabbit.Core.PipelineClient
{
    public static class Program
    {
        public static async Task Main()
        {
            await RunPipelineExecutionAsync()
                .ConfigureAwait(false);
        }

        private static async Task RunPipelineExecutionAsync()
        {
            await Console.Out.WriteLineAsync("Starting PipelineClient...").ConfigureAwait(false);

            var rabbitService = await SetupAsync().ConfigureAwait(false);

            // Start Consumer As An Execution Engine
            var consumer = rabbitService.GetLetterConsumer("ConsumerFromConfig");
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);

            const int maxDegreesOfParallelism = 32;
            var workflow = BuildPipeline(maxDegreesOfParallelism);

            _ = Task.Run(() => consumer.PipelineExecutionEngineAsync(workflow, false));

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task<RabbitService> SetupAsync()
        {
            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());
            var rabbitService = new RabbitService("Config.json");

            await rabbitService
                .InitializeAsync("passwordforencryption", "saltforencryption")
                .ConfigureAwait(false);

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            // Produce Messages
            for (ulong i = 0; i < 100; i++)
            {
                var letter = letterTemplate.Clone();
                letter.LetterId = i;
                var sentMessage = new Message { StringMessage = "Sensitive Message" };
                sentMessage.StringMessage += $" {i}";
                letter.Body = JsonSerializer.SerializeToUtf8Bytes(sentMessage);
                await rabbitService
                    .AutoPublisher
                    .QueueLetterAsync(letter);
            }

            return rabbitService;
        }

        public static Pipeline<ReceivedLetter, WorkState> BuildPipeline(int maxDegreesOfParallelism)
        {
            var pipeline = new Pipeline<ReceivedLetter, WorkState>(maxDegreesOfParallelism);
            pipeline.AddStep<ReceivedLetter, WorkState>(DeserializeStep);
            pipeline.AddAsyncStep<WorkState, WorkState>(ProcessStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(AckMessageAsync);

            pipeline
                .Finalize((state) =>
                {
                    if (state.AllStepsSuccess)
                    { Console.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Finished route successful."); }
                    else
                    { Console.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Finished route unsuccesfully."); }

                    // Lastly mark the excution pipeline finished for this message.
                    state.ReceivedLetter.Complete(); // This impacts wait to completion step in the WorkFlowEngine.
                });

            return pipeline;
        }

        public class Message
        {
            public string StringMessage { get; set; }
        }

        public class WorkState
        {
            public Message Message { get; set; }
            public ReceivedLetter ReceivedLetter { get; set; }
            public ulong LetterId { get; set; }
            public bool DeserializeStepSuccess { get; set; }
            public bool ProcessStepSuccess { get; set; }
            public bool AcknowledgeStepSuccess { get; set; }
            public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
        }

        private static WorkState DeserializeStep(ReceivedLetter receivedLetter)
        {
            var state = new WorkState();
            try
            {
                var decodedLetter = JsonSerializer.Deserialize<Message>(receivedLetter.Letter.Body);
                state.ReceivedLetter = receivedLetter;
                state.LetterId = receivedLetter.Letter.LetterId;
                state.Message = decodedLetter;
                state.DeserializeStepSuccess = true;
            }
            catch
            { state.DeserializeStepSuccess = false; }

            return state;
        }

        private static async Task<WorkState> ProcessStepAsync(WorkState state)
        {
            await Console
                .Out
                .WriteLineAsync($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Deserialize Step Success? {state.DeserializeStepSuccess}")
                .ConfigureAwait(false);

            if (state.DeserializeStepSuccess)
            {
                await Console
                    .Out
                    .WriteLineAsync($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Received: {state.Message.StringMessage}")
                    .ConfigureAwait(false);

                state.ProcessStepSuccess = true;
            }

            return state;
        }

        private static async Task<WorkState> AckMessageAsync(WorkState state)
        {
            await Console
                .Out
                .WriteLineAsync($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Process Step Success? {state.ProcessStepSuccess}")
                .ConfigureAwait(false);

            if (state.ProcessStepSuccess)
            {
                await Console
                    .Out
                    .WriteLineAsync($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Acking message...")
                    .ConfigureAwait(false);

                if (state.ReceivedLetter.AckMessage())
                { state.AcknowledgeStepSuccess = true; }
            }
            else
            {
                await Console
                    .Out
                    .WriteLineAsync($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Nacking message...")
                    .ConfigureAwait(false);

                if (state.ReceivedLetter.NackMessage(true))
                { state.AcknowledgeStepSuccess = true; }
            }

            return state;
        }
    }
}
