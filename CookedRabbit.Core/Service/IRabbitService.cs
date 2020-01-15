using System.Collections.Concurrent;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;

namespace CookedRabbit.Core.Service
{
    public interface IRabbitService
    {
        AutoPublisher AutoPublisher { get; }
        ChannelPool ChannelPool { get; }
        bool Initialized { get; }
        ConcurrentDictionary<string, LetterConsumer> LetterConsumers { get; }
        ConcurrentDictionary<string, MessageConsumer> MessageConsumers { get; }
        Topologer Topologer { get; }

        LetterConsumer GetLetterConsumer(string consumerName);
        MessageConsumer GetMessageConsumer(string consumerName);
        Task InitializeAsync();
        Task InitializeAsync(string passphrase, string salt);
        ValueTask ShutdownAsync(bool immediately);
    }
}