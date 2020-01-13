using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    public class WorkflowEngine<TIn, TOut>
    {
        private Workflow<TIn, TOut> Pipeline { get; }
        private ActionBlock<TIn> Block { get; }

        public WorkflowEngine(
            Workflow<TIn, TOut> pipeline,
            int maxDegreeOfParallelism)
        {
            Pipeline = pipeline;
            Block = new ActionBlock<TIn>(
                ExecuteWorkBodyAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism
                });
        }

        private async Task ExecuteWorkBodyAsync(TIn input)
        {
            await Pipeline
                .BeginAsync(input)
                .ConfigureAwait(false);
        }

        public async ValueTask EnqueueWorkAsync(TIn input)
        {
            try
            { await Block.SendAsync(input).ConfigureAwait(false); }
            catch
            { }
        }
    }
}
