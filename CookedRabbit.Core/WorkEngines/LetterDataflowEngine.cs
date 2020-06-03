using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    public class LetterDataflowEngine
    {
        private ActionBlock<ReceivedLetter> Block { get; }

        private Func<ReceivedLetter, Task<bool>> WorkBodyAsync { get; }

        public LetterDataflowEngine(Func<ReceivedLetter, Task<bool>> workBodyAsync, int maxDegreeOfParallelism)
        {
            WorkBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));
            Block = new ActionBlock<ReceivedLetter>(
                ExecuteWorkBodyAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism
                });
        }

        private async Task ExecuteWorkBodyAsync(ReceivedLetter receivedLetter)
        {
            try
            {
                if (await WorkBodyAsync(receivedLetter).ConfigureAwait(false))
                { receivedLetter.AckMessage(); }
                else
                { receivedLetter.NackMessage(true); }
            }
            catch
            { receivedLetter.NackMessage(true); }
        }

        public async ValueTask EnqueueWorkAsync(ReceivedLetter receivedLetter)
        {
            try
            { await Block.SendAsync(receivedLetter).ConfigureAwait(false); }
            catch
            { }
        }
    }
}
