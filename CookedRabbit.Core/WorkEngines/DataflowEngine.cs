using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    public class DataflowEngine
    {
        private ActionBlock<ReceivedData> Block { get; }

        private Func<ReceivedData, Task<bool>> WorkBodyAsync { get; }

        public DataflowEngine(Func<ReceivedData, Task<bool>> workBodyAsync, int maxDegreeOfParallelism)
        {
            WorkBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));
            Block = new ActionBlock<ReceivedData>(
                ExecuteWorkBodyAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism
                });
        }

        private async Task ExecuteWorkBodyAsync(ReceivedData receivedData)
        {
            try
            {
                if (await WorkBodyAsync(receivedData).ConfigureAwait(false))
                { receivedData.AckMessage(); }
                else
                { receivedData.NackMessage(true); }
            }
            catch
            { receivedData.NackMessage(true); }
        }

        public async ValueTask EnqueueWorkAsync(ReceivedData receivedData)
        {
            try
            { await Block.SendAsync(receivedData).ConfigureAwait(false); }
            catch
            { }
        }
    }
}
