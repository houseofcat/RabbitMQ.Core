using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    public class LetterDataflowEngine
    {
        private readonly ILogger<LetterDataflowEngine> _logger;
        private readonly ActionBlock<ReceivedLetter> _block;
        private readonly Func<ReceivedLetter, Task<bool>> _workBodyAsync;

        public LetterDataflowEngine(Func<ReceivedLetter, Task<bool>> workBodyAsync, int maxDegreeOfParallelism)
        {
            _logger = LogHelper.GetLogger<LetterDataflowEngine>();

            _workBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));
            _block = new ActionBlock<ReceivedLetter>(
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
                _logger.LogDebug(
                    LogMessages.LetterDataflowEngine.Execution,
                    receivedLetter.Letter.LetterId,
                    receivedLetter.DeliveryTag);

                if (await _workBodyAsync(receivedLetter).ConfigureAwait(false))
                {
                    _logger.LogDebug(
                        LogMessages.LetterDataflowEngine.ExecutionSuccess,
                        receivedLetter.Letter.LetterId,
                        receivedLetter.DeliveryTag);

                    receivedLetter.AckMessage();
                }
                else
                {
                    _logger.LogWarning(
                        LogMessages.LetterDataflowEngine.ExecutionFailure,
                        receivedLetter.Letter.LetterId,
                        receivedLetter.DeliveryTag);

                    receivedLetter.NackMessage(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.LetterDataflowEngine.ExecutionError,
                    receivedLetter.Letter.LetterId,
                    receivedLetter.DeliveryTag,
                    ex.Message);

                receivedLetter.NackMessage(true);
            }
        }

        public async ValueTask EnqueueWorkAsync(ReceivedLetter receivedLetter)
        {
            try
            { await _block.SendAsync(receivedLetter).ConfigureAwait(false); }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogMessages.LetterDataflowEngine.ExecutionError,
                    receivedLetter.Letter.LetterId,
                    receivedLetter.DeliveryTag,
                    ex.Message);
            }
        }
    }
}
