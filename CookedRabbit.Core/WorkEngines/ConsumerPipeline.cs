using System;
using System.Threading;
using System.Threading.Tasks;

namespace CookedRabbit.Core.WorkEngines
{
    public interface IConsumerPipeline<TOut> where TOut : IWorkState
    {
        string ConsumerPipelineName { get; }
        ConsumerOptions Options { get; }

        Task AwaitCompletionAsync();
        Task StartAsync(bool useStream);
        Task StopAsync();
    }

    public class ConsumerPipeline<TOut> : IConsumerPipeline<TOut> where TOut : IWorkState
    {
        public string ConsumerPipelineName { get; }
        public ConsumerOptions Options { get; }

        private IConsumer<ReceivedData> Consumer { get; }
        private IPipeline<ReceivedData, TOut> Pipeline { get; }
        private Task FeedPipelineWithDataTasks { get; set; }
        private TaskCompletionSource<bool> _completionSource;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _started;

        private readonly SemaphoreSlim cpLock = new SemaphoreSlim(1, 1);

        public ConsumerPipeline(
            IConsumer<ReceivedData> consumer,
            IPipeline<ReceivedData, TOut> pipeline)
        {
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            Options = consumer.ConsumerSettings ?? throw new ArgumentNullException(nameof(consumer.ConsumerSettings));
            if (consumer.ConsumerSettings.ConsumerPipelineSettings == null) throw new ArgumentNullException(nameof(consumer.ConsumerSettings.ConsumerPipelineSettings));

            ConsumerPipelineName = !string.IsNullOrWhiteSpace(consumer.ConsumerSettings.ConsumerPipelineSettings.ConsumerPipelineName)
                ? consumer.ConsumerSettings.ConsumerPipelineSettings.ConsumerPipelineName
                : "Unknown";
        }

        public async Task StartAsync(bool useStream)
        {
            await cpLock.WaitAsync();

            try
            {
                if (!_started)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    _completionSource = new TaskCompletionSource<bool>();

                    await Consumer
                        .StartConsumerAsync(
                            Options.AutoAck.Value,
                            Options.UseTransientChannels.Value)
                        .ConfigureAwait(false);

                    if (Consumer.Started)
                    {
                        if (useStream)
                        {
                            FeedPipelineWithDataTasks = Task.Run(
                                () =>
                                Consumer.PipelineStreamEngineAsync(
                                    Pipeline,
                                    Options.ConsumerPipelineSettings.WaitForCompletion.Value,
                                    _cancellationTokenSource.Token));
                        }
                        else
                        {
                            FeedPipelineWithDataTasks = Task.Run(
                                () =>
                                Consumer.PipelineExecutionEngineAsync(
                                    Pipeline,
                                    Options.ConsumerPipelineSettings.WaitForCompletion.Value,
                                    _cancellationTokenSource.Token));
                        }

                        _started = true;
                    }
                }
            }
            catch { }
            finally
            { cpLock.Release(); }
        }

        public async Task StopAsync()
        {
            await cpLock.WaitAsync();

            try
            {
                if (_started)
                {
                    _cancellationTokenSource.Cancel();

                    await Consumer
                        .StopConsumerAsync(false);

                    if (FeedPipelineWithDataTasks != null)
                    {
                        await FeedPipelineWithDataTasks;
                        FeedPipelineWithDataTasks = null;
                    }
                    _started = false;
                    _completionSource.SetResult(true);
                }
            }
            catch { }
            finally { cpLock.Release(); }
        }

        public async Task AwaitCompletionAsync()
        {
            await _completionSource.Task;
        }
    }
}
