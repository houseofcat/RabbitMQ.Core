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
            var consumerPipelineExample = new ConsumerPipelineExample();
            await consumerPipelineExample
                .RunPipelineExecutionAsync()
                .ConfigureAwait(false);
        }
    }

    public class ConsumerPipelineExample
    {
        private string _errorQueue;
        private IRabbitService _rabbitService;
        private ILogger<ConsumerPipelineExample> _logger;

        private static Task PublisherOne { get; set; }
        private static Task PublisherTwo { get; set; }

        public async Task RunPipelineExecutionAsync()
        {
            await Console.Out.WriteLineAsync("Starting ConsumerPipelineExample...").ConfigureAwait(false);

            _rabbitService = await SetupAsync().ConfigureAwait(false);
            _errorQueue = _rabbitService.Config.GetConsumerSettings("ConsumerFromConfig").ErrorQueueName;

            var consumerPipeline = _rabbitService.CreateConsumerPipeline("ConsumerFromConfig", 32, BuildPipeline);
            await consumerPipeline.StartAsync().ConfigureAwait(false);

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private async Task<RabbitService> SetupAsync()
        {
            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
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

            //for (ulong i = 0; i < 100; i++)
            //{
            //    var letter = letterTemplate.Clone();
            //    letter.Body = JsonSerializer.SerializeToUtf8Bytes(new Message { StringMessage = $"Sensitive ReceivedLetter {i}", MessageId = i });
            //    letter.LetterId = i;
            //    await rabbitService
            //        .AutoPublisher
            //        .Publisher
            //        .PublishAsync(letter, true, true)
            //        .ConfigureAwait(false);
            //}

            PublisherOne = Task.Run(async () =>
            {
                // Produce ReceivedLetters
                for (ulong i = 0; i < 50_000; i++)
                {
                    var letter = letterTemplate.Clone();
                    letter.Body = JsonSerializer.SerializeToUtf8Bytes(new Message { StringMessage = $"Sensitive ReceivedLetter {i}", MessageId = i });
                    letter.LetterId = i;
                    await rabbitService
                        .AutoPublisher
                        .QueueLetterAsync(letter);
                }
            });

            PublisherTwo = Task.Run(async () =>
            {
                // Produce ReceiveMessages
                for (ulong i = 50_000; i < 100_000; i++)
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

        public IPipeline<ReceivedData, WorkState> BuildPipeline(int maxDoP)
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
                    state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.
                });

            return pipeline;
        }

        public class Message
        {
            public ulong MessageId { get; set; }
            public string StringMessage { get; set; }
        }

        public class WorkState : WorkEngines.WorkState
        {
            public Message Message { get; set; }
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
            else
            {
                var failed = await _rabbitService
                    .AutoPublisher
                    .Publisher
                    .PublishAsync("", _errorQueue, state.ReceivedData.Data, null)
                    .ConfigureAwait(false);

                if (failed)
                {
                    _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize and publish to ErrorQueue!\r\n{await state.ReceivedData.GetBodyAsUtf8StringAsync().ConfigureAwait(false)}\r\n");
                }
                else
                {
                    _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize. Published to ErrorQueue!\r\n{await state.ReceivedData.GetBodyAsUtf8StringAsync().ConfigureAwait(false)}\r\n");

                    // So we ack the message
                    state.ProcessStepSuccess = true;
                }
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
