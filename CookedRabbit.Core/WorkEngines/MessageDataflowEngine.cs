using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    public class MessageDataflowEngine
    {
        private ILogger<MessageDataflowEngine> _logger;
        private ActionBlock<ReceivedMessage> _block;
        private Func<ReceivedMessage, Task<bool>> _workBodyAsync;

        public MessageDataflowEngine(Func<ReceivedMessage, Task<bool>> workBodyAsync, int maxDegreeOfParallelism)
        {
            _logger = LogHelper.GetLogger<MessageDataflowEngine>();

            _workBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));
            _block = new ActionBlock<ReceivedMessage>(
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
                _logger.LogDebug(
                    LogMessages.DataflowEngine.Execution,
                    receivedMessage.DeliveryTag);

                if (await _workBodyAsync(receivedMessage).ConfigureAwait(false))
                {
                    _logger.LogDebug(
                        LogMessages.DataflowEngine.ExecutionSuccess,
                        receivedMessage.DeliveryTag);

                    receivedMessage.AckMessage();
                }
                else
                {
                    _logger.LogWarning(
                        LogMessages.DataflowEngine.ExecutionFailure,
                        receivedMessage.DeliveryTag);

                    receivedMessage.NackMessage(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.DataflowEngine.ExecutionError,
                    receivedMessage.DeliveryTag,
                    ex.Message);

                receivedMessage.NackMessage(true);
            }
        }

        public async ValueTask EnqueueWorkAsync(ReceivedMessage receivedLetter)
        {
            try
            { await _block.SendAsync(receivedLetter).ConfigureAwait(false); }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.DataflowEngine.ExecutionError,
                    receivedLetter.DeliveryTag,
                    ex.Message);
            }
        }
    }
}
