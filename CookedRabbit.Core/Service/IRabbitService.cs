
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;

namespace CookedRabbit.Core.Service
{
    public interface IRabbitService
    {
        AutoPublisher AutoPublisher { get; }
        ChannelPool ChannelPool { get; }
        bool Initialized { get; }
        Topologer Topologer { get; }

        LetterConsumer GetConsumer(string consumerName);
        Task InitializeAsync();
        Task InitializeAsync(string passphrase, string salt);
    }
}