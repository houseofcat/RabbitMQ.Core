using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    public class MessageDataflowEngine
    {
        private ActionBlock<ReceivedMessage> Block { get; }

        private Func<ReceivedMessage, Task<bool>> WorkBodyAsync { get; }

        public MessageDataflowEngine(Func<ReceivedMessage, Task<bool>> workBodyAsync, int maxDegreeOfParallelism)
        {
            WorkBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));
            Block = new ActionBlock<ReceivedMessage>(
                ExecuteWorkBodyAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism
                });
        }

        private async Task ExecuteWorkBodyAsync(ReceivedMessage receivedMessage)
        {
            try
            {
                if (await WorkBodyAsync(receivedMessage).ConfigureAwait(false))
                { receivedMessage.AckMessage(); }
                else
                { receivedMessage.NackMessage(true); }
            }
            catch
            { receivedMessage.NackMessage(true); }
        }

        public async ValueTask EnqueueWorkAsync(ReceivedMessage receivedMessage)
        {
            try
            { await Block.SendAsync(receivedMessage).ConfigureAwait(false); }
            catch
            { }
        }
    }
}
