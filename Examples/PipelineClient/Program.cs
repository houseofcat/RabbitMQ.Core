using CookedRabbit.Core.Service;
using CookedRabbit.Core.WorkEngines;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CookedRabbit.Core.PipelineClient
{
    public static class Program
    {
        public static async Task Main()
        {
            //await LetterConsumerPipelineExample
            //    .RunPipelineExecutionAsync()
            //    .ConfigureAwait(false);

            var consumerPipelineExample = new ConsumerPipelineExample();
            await consumerPipelineExample
                .RunPipelineExecutionAsync()
                .ConfigureAwait(false);
        }
    }

    public static class LetterConsumerPipelineExample
    {
        public static async Task RunPipelineExecutionAsync()
        {
            await Console.Out.WriteLineAsync("Starting LetterConsumerPipelineExample...").ConfigureAwait(false);

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

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
            var rabbitService = new RabbitService("Config.json", loggerFactory);

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
                letter.Body = JsonSerializer.SerializeToUtf8Bytes(new Message { StringMessage = $"Sensitive Message {i}" });

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
                    { Console.WriteLine($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - LetterId: {state.LetterId} - Finished route successfully."); }
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
                var decodedLetter = JsonSerializer.Deserialize<Message>(receivedLetter.Letter.Body.AsSpan());
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

    public class ConsumerPipelineExample
    {
        private string _errorQueue;
        private IRabbitService _rabbitService;
        private ILogger<ConsumerPipelineExample> _logger;

        public async Task RunPipelineExecutionAsync()
        {
            await Console.Out.WriteLineAsync("Starting ConsumerPipelineExample...").ConfigureAwait(false);

            _rabbitService = await SetupAsync()
                .ConfigureAwait(false);

            // Start Consumer As An Execution Engine
            var consumer = _rabbitService.GetConsumer("ConsumerFromConfig");
            _errorQueue = consumer.ConsumerSettings.ErrorQueueName;
            await consumer
                .StartConsumerAsync(false, true)
                .ConfigureAwait(false);

            const int maxDoP = 32;
            var workflow = BuildPipeline(maxDoP);

            _ = Task.Run(() => consumer.PipelineExecutionEngineAsync(workflow, false));

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static Task PublisherOne { get; set; }
        private static Task PublisherTwo { get; set; }
        private async Task<RabbitService> SetupAsync()
        {
            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
            _logger = loggerFactory.CreateLogger<ConsumerPipelineExample>();
            var rabbitService = new RabbitService(
                "Config.json",
                loggerFactory);

            await rabbitService
                .InitializeAsync("passwordforencryption", "saltforencryption")
                .ConfigureAwait(false);

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            PublisherOne = Task.Run(async () =>
            {
                // Produce ReceivedLetters
                for (ulong i = 0; i < 10_000; i++)
                {
                    var letter = letterTemplate.Clone();
                    letter.Body = JsonSerializer.SerializeToUtf8Bytes(new Message { StringMessage = $"Sensitive ReceivedLetter {i}", MessageId = i });

                    await rabbitService
                        .AutoPublisher
                        .QueueLetterAsync(letter);
                }
            });

            PublisherTwo = Task.Run(async () =>
            {
                // Produce ReceiveMessages
                for (ulong i = 10_000; i < 20_000; i++)
                {
                    var sentMessage = new Message { StringMessage = $"Sensitive ReceivedMessage {i}", MessageId = i };
                    await rabbitService
                        .AutoPublisher
                        .Publisher
                        .PublishAsync("", "TestRabbitServiceQueue", JsonSerializer.SerializeToUtf8Bytes(sentMessage), null)
                        .ConfigureAwait(false);
                }
            });

            return rabbitService;
        }

        public Pipeline<ReceivedData, WorkState> BuildPipeline(int maxDoP)
        {
            var pipeline = new Pipeline<ReceivedData, WorkState>(
                maxDoP,
                healthCheckInterval: TimeSpan.FromSeconds(10),
                pipelineName: "ConsumerPipelineExample");

            pipeline.AddAsyncStep<ReceivedData, WorkState>(DeserializeStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(ProcessStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(AckMessageAsync);

            pipeline
                .Finalize((state) =>
                {
                    if (state.AllStepsSuccess)
                    { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route successfully."); }
                    else
                    { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route unsuccesfully."); }

                    // Lastly mark the excution pipeline finished for this message.
                    state.ReceivedData?.Complete(); // This impacts wait to completion step in the WorkFlowEngine.
                });

            return pipeline;
        }

        public class Message
        {
            public ulong MessageId { get; set; }
            public string StringMessage { get; set; }
        }

        public class WorkState
        {
            public Message Message { get; set; }
            public IReceivedData ReceivedData { get; set; }
            public ulong LetterId { get; set; }
            public bool DeserializeStepSuccess { get; set; }
            public bool ProcessStepSuccess { get; set; }
            public bool AcknowledgeStepSuccess { get; set; }
            public bool AllStepsSuccess => DeserializeStepSuccess && ProcessStepSuccess && AcknowledgeStepSuccess;
        }

        private async Task<WorkState> DeserializeStepAsync(IReceivedData receivedData)
        {
            var state = new WorkState
            {
                ReceivedData = receivedData
            };

            try
            {
                state.Message = state.ReceivedData.ContentType switch
                {
                    Constants.HeaderValueForLetter => await receivedData
                        .GetTypeFromJsonAsync<Message>()
                        .ConfigureAwait(false),

                    _ => await receivedData
                        .GetTypeFromJsonAsync<Message>(decrypt: false, decompress: false)
                        .ConfigureAwait(false),
                };

                if (state.ReceivedData.Data.Length > 0 && (state.Message != null || state.ReceivedData.Letter != null))
                { state.DeserializeStepSuccess = true; }
            }
            catch
            { }

            if (!state.DeserializeStepSuccess)
            {
                // Park Failed Deserialize Steps
                var failed = await _rabbitService
                    .AutoPublisher
                    .Publisher
                    .PublishAsync("", _errorQueue, state.ReceivedData.Data, null)
                    .ConfigureAwait(false);

                if (failed)
                {
                    _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize and publish to ErrorQueue!\r\n{state.ReceivedData.GetBodyAsUtf8String()}\r\n");
                }
                else
                {
                    _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize. Published to ErrorQueue!\r\n{state.ReceivedData.GetBodyAsUtf8String()}\r\n");

                    // So we ack the message
                    state.ProcessStepSuccess = true;
                }
            }

            return state;
        }

        private async Task<WorkState> ProcessStepAsync(WorkState state)
        {
            await Task.Yield();

            _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Deserialize Step Success? {state.DeserializeStepSuccess}");

            if (state.DeserializeStepSuccess)
            {
                _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Received: {state.Message?.StringMessage}");

                state.ProcessStepSuccess = true;
            }

            return state;
        }

        private async Task<WorkState> AckMessageAsync(WorkState state)
        {
            await Task.Yield();

            _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Process Step Success? {state.ProcessStepSuccess}");

            if (state.ProcessStepSuccess)
            {
                _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Acking message...");

                if (state.ReceivedData.AckMessage())
                { state.AcknowledgeStepSuccess = true; }
            }
            else
            {
                _logger.LogDebug($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Nacking message...");

                if (state.ReceivedData.NackMessage(true))
                { state.AcknowledgeStepSuccess = true; }
            }

            return state;
        }
    }
}
