using System.Threading.Tasks;
using CookedRabbit.Core.Pools;

namespace CookedRabbit.Core.Service
{
    public interface IMaintenanceService
    {
        Task<bool> PurgeQueueAsync(ChannelPool channelPool, string queueName, bool deleteQueueAfter = false);
        Task<bool> TransferAllMessagesAsync(ChannelPool originChannelPool, ChannelPool targetChannelPool, string originQueueName, string targetQueueName);
        Task<bool> TransferAllMessagesAsync(ChannelPool channelPool, string originQueueName, string targetQueueName);
        Task<bool> TransferMessageAsync(ChannelPool originChannelPool, ChannelPool targetChannelPool, string originQueueName, string targetQueueName);
        Task<bool> TransferMessageAsync(ChannelPool channelPool, string originQueueName, string targetQueueName);
    }
}