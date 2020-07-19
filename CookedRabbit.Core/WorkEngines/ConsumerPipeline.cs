using System;
using System.Threading;
using System.Threading.Tasks;

namespace CookedRabbit.Core.WorkEngines
{
    public interface IConsumerPipeline<TOut> where TOut : IWorkState
    {
        string ConsumerPipelineName { get; }
        ConsumerOptions Options { get; set; }

        Task StartAsync();
        Task StopAsync();
    }

    public class ConsumerPipeline<TOut> : IConsumerPipeline<TOut> where TOut : IWorkState
    {
        public string ConsumerPipelineName { get; }
        public ConsumerOptions Options { get; set; }

        private IConsumer<ReceivedData> Consumer { get; }
        private IPipeline<ReceivedData, TOut> Pipeline { get; }
        private Task FeedPipelineWithDataTasks { get; set; }
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim cpLock = new SemaphoreSlim(1, 1);
        private bool _started;

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

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            await cpLock.WaitAsync();

            try
            {
                await Consumer
                    .StartConsumerAsync(
                        Options.AutoAck.Value,
                        Options.UseTransientChannels.Value)
                    .ConfigureAwait(false);

                if (Consumer.Consuming)
                {
                    _started = true;
                    FeedPipelineWithDataTasks = FeedThePipelineEngineAsync(_cancellationTokenSource.Token);
                }
            }
            catch { }
            finally
            { cpLock.Release(); }
        }

        public async Task StopAsync()
        {
            if (_started)
            {
                await cpLock.WaitAsync();

                try
                {
                    if (_started)
                    {
                        _cancellationTokenSource.Cancel();
                        if (FeedPipelineWithDataTasks != null)
                        {
                            await FeedPipelineWithDataTasks;
                            FeedPipelineWithDataTasks = null;
                        }
                        _started = false;
                    }
                }
                catch { }
                finally { cpLock.Release(); }
            }
        }

        private async Task FeedThePipelineEngineAsync(CancellationToken token)
        {
            while (_started)
            {
                if (token.IsCancellationRequested)
                { return; }

                await Consumer
                    .PipelineExecutionEngineAsync(Pipeline, Options.ConsumerPipelineSettings.WaitForCompletion.Value, token);

                await Task.Delay(Options.SleepOnIdleInterval.Value);
            }
        }
    }
}
