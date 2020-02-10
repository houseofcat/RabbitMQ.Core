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

        Task ComcryptAsync(ReceivedLetter receivedLetter);
        Task<bool> CompressAsync(ReceivedLetter receivedLetter);
        Task DecomcryptAsync(ReceivedLetter receivedLetter);
        Task<bool> DecompressAsync(ReceivedLetter receivedLetter);
        bool Decrypt(ReceivedLetter receivedLetter);
        bool Encrypt(ReceivedLetter receivedLetter);
        LetterConsumer GetLetterConsumer(string consumerName);
        MessageConsumer GetMessageConsumer(string consumerName);
        Task InitializeAsync();
        Task InitializeAsync(string passphrase, string salt);
        ValueTask ShutdownAsync(bool immediately);
    }
}