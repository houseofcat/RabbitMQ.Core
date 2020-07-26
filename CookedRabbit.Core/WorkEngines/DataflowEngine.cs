using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    public class DataflowEngine
    {
        private readonly ILogger<DataflowEngine> _logger;
        private readonly ActionBlock<ReceivedData> _block;
        private readonly Func<ReceivedData, Task<bool>> _workBodyAsync;

        public DataflowEngine(Func<ReceivedData, Task<bool>> workBodyAsync, int maxDegreeOfParallelism)
        {
            _logger = LogHelper.GetLogger<DataflowEngine>();

            _workBodyAsync = workBodyAsync ?? throw new ArgumentNullException(nameof(workBodyAsync));
            _block = new ActionBlock<ReceivedData>(
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
                _logger.LogDebug(
                    LogMessages.DataflowEngine.Execution,
                    receivedData.DeliveryTag);

                if (await _workBodyAsync(receivedData).ConfigureAwait(false))
                {
                    _logger.LogDebug(
                        LogMessages.DataflowEngine.ExecutionSuccess,
                        receivedData.DeliveryTag);

                    receivedData.AckMessage();
                }
                else
                {
                    _logger.LogWarning(
                        LogMessages.DataflowEngine.ExecutionFailure,
                        receivedData.DeliveryTag);

                    receivedData.NackMessage(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    LogMessages.DataflowEngine.ExecutionError,
                    receivedData.DeliveryTag,
                    ex.Message);

                receivedData.NackMessage(true);
            }
        }

        public async ValueTask EnqueueWorkAsync(ReceivedData receivedData)
        {
            await _block.SendAsync(receivedData).ConfigureAwait(false);
        }
    }
}
