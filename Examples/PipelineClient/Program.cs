using CookedRabbit.Core.Service;
using CookedRabbit.Core.WorkEngines;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CookedRabbit.Core.PipelineClient
{
    public static class Program
    {
        public static Stopwatch Stopwatch;
        public static LogLevel LogLevel = LogLevel.Trace;
        public static long GlobalCount = 1000;
        public static bool EnsureOrdered = true;
        public static bool AwaitShutdown = true;
        public static bool LogOutcome = false;
        public static bool UseStreamPipeline = false;
        public static int MaxDoP = 64;
        public static Random Rand = new Random();

        public static async Task Main()
        {
            var consumerPipelineExample = new ConsumerPipelineExample();
            await Console.Out.WriteLineAsync("About to run Client... press a key to continue!");
            await Console.In.ReadLineAsync(); // memory snapshot baseline

            await consumerPipelineExample
                .RunExampleAsync()
                .ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Statistics!");
            await Console.Out.WriteLineAsync($"MaxDoP: {MaxDoP}, Ensure Ordered: {EnsureOrdered}");
            await Console.Out.WriteLineAsync($"AwaitShutdown: {AwaitShutdown}, LogOutcome: {LogOutcome}");
            await Console.Out.WriteLineAsync($"UseStreamPipeline: {UseStreamPipeline}");
            await Console.Out.WriteLineAsync($"Finished processing {GlobalCount} messages in {Stopwatch.ElapsedMilliseconds} milliseconds.");
            await Console.Out.WriteLineAsync($"Rate {GlobalCount / (Stopwatch.ElapsedMilliseconds / 1.0) * 1000.0} msg/s.");
            await Console.Out.WriteLineAsync("Client Finished! Press key to start shutdown!");
            await Console.In.ReadLineAsync(); // checking for memory leak (snapshots)
            await consumerPipelineExample.ShutdownAsync();
            await Console.Out.WriteLineAsync("All finished cleanup!");
            await Console.In.ReadLineAsync().ConfigureAwait(false); // checking for memory leak (snapshots)
        }
    }

    public class ConsumerPipelineExample
    {
        private IRabbitService _rabbitService;
        private ILogger<ConsumerPipelineExample> _logger;
        private IConsumerPipeline<WorkState> _consumerPipeline;

        private static Task PublisherOne { get; set; }
        private static Task PublisherTwo { get; set; }

        private string _errorQueue;
        private long _targetCount;
        private long _currentMessageCount;

        public async Task RunExampleAsync()
        {
            _targetCount = Program.GlobalCount;
            await Console.Out.WriteLineAsync("Running example...").ConfigureAwait(false);

            _rabbitService = await SetupAsync().ConfigureAwait(false);
            _errorQueue = _rabbitService.Config.GetConsumerSettings("ConsumerFromConfig").ErrorQueueName;

            _consumerPipeline = _rabbitService.CreateConsumerPipeline("ConsumerFromConfig", Program.MaxDoP, Program.EnsureOrdered, BuildPipeline);
            Program.Stopwatch = Stopwatch.StartNew();
            await _consumerPipeline.StartAsync(Program.UseStreamPipeline).ConfigureAwait(false);
            if (Program.AwaitShutdown)
            {
                await Console.Out.WriteLineAsync("Awaiting full ConsumerPipeline finish...").ConfigureAwait(false);
                await _consumerPipeline.AwaitCompletionAsync();
            }
            Program.Stopwatch.Stop();
            await Console.Out.WriteLineAsync("Example finished...").ConfigureAwait(false);
        }

        public async Task ShutdownAsync()
        {
            await _rabbitService.ShutdownAsync(false);
        }

        private async Task<RabbitService> SetupAsync()
        {
            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(Program.LogLevel));
            _logger = loggerFactory.CreateLogger<ConsumerPipelineExample>();
            var rabbitService = new RabbitService(
                "Config.json",
                "passwordforencryption",
                "saltforencryption",
                loggerFactory);

            await rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);

            for (var i = 0L; i < _targetCount; i++)
            {
                var letter = letterTemplate.Clone();
                letter.Body = JsonSerializer.SerializeToUtf8Bytes(new Message { StringMessage = $"Sensitive ReceivedLetter {i}", MessageId = i });
                letter.LetterId = (ulong)i;
                await rabbitService
                    .Publisher
                    .PublishAsync(letter, true, true)
                    .ConfigureAwait(false);
            }

            //PublisherTwo = Task.Run(
            //    async () =>
            //    {
            //        await Task.Yield();
            //        for (ulong i = 100; i < 200; i++)
            //        {
            //            var sentMessage = new Message { StringMessage = $"Sensitive ReceivedMessage {i}", MessageId = i };
            //            await rabbitService
            //                .Publisher
            //                .PublishAsync("", "TestRabbitServiceQueue", JsonSerializer.SerializeToUtf8Bytes(sentMessage), null)
            //                .ConfigureAwait(false);
            //        }
            //    });

            return rabbitService;
        }

        private IPipeline<ReceivedData, WorkState> BuildPipeline(int maxDoP, bool? ensureOrdered = null)
        {
            var pipeline = new Pipeline<ReceivedData, WorkState>(
                maxDoP,
                healthCheckInterval: TimeSpan.FromSeconds(10),
                pipelineName: "ConsumerPipelineExample",
                ensureOrdered);

            pipeline.AddAsyncStep<ReceivedData, WorkState>(DeserializeStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(ProcessStepAsync);
            pipeline.AddAsyncStep<WorkState, WorkState>(AckMessageAsync);

            pipeline
                .Finalize(async (state) =>
                {
                    if (Program.LogOutcome)
                    {
                        if (state.AllStepsSuccess)
                        { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route successfully."); }
                        else
                        { _logger.LogInformation($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - Id: {state.Message?.MessageId} - Finished route unsuccesfully."); }
                    }

                    // Lastly mark the excution pipeline finished for this message.
                    state.ReceivedData?.Complete(); // This impacts wait to completion step in the Pipeline.

                    if (Program.AwaitShutdown)
                    {
                        Interlocked.Increment(ref _currentMessageCount);
                        if (_currentMessageCount == _targetCount - 1)
                        {

                            await _consumerPipeline.StopAsync();
                        }
                    }
                });

            return pipeline;
        }

        public class Message
        {
            public long MessageId { get; set; }
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

                // Simulate processing.
                await Task.Delay(Program.Rand.Next(1, 100));
            }
            else
            {
                var failed = await _rabbitService
                    .Publisher
                    .PublishAsync("", _errorQueue, state.ReceivedData.Data, null)
                    .ConfigureAwait(false);

                var stringBody = string.Empty;

                try
                { stringBody = await state.ReceivedData.GetBodyAsUtf8StringAsync().ConfigureAwait(false); }
                catch (Exception ex) { _logger.LogError(ex, "What?!"); }

                if (failed)
                {
                    _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize and publish to ErrorQueue!\r\n{stringBody}\r\n");
                }
                else
                {
                    _logger.LogError($"{DateTime.Now:yyyy/MM/dd hh:mm:ss.fff} - This failed to deserialize. Published to ErrorQueue!\r\n{stringBody}\r\n");

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

    public static class Extensions
    {
        private const long Billion = 1_000_000_000L;
        private const long Million = 1_000_000L;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ElapsedNanoseconds(this Stopwatch watch)
        {
            return (long)((double)watch.ElapsedTicks / Stopwatch.Frequency * Billion);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ElapsedMicroseconds(this Stopwatch watch)
        {
            return (long)((double)watch.ElapsedTicks / Stopwatch.Frequency * Million);
        }
    }
}
