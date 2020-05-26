using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CookedRabbit.Core
{
    public class ReceivedLetter : ReceivedData
    {
        public Letter Letter { get; private set; }

        private TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();

        public ReceivedLetter(IModel channel, BasicGetResult result, bool ackable) : base(channel, result, ackable)
        {
            Letter = JsonSerializer.Deserialize<Letter>(result.Body.Span);
        }

        public ReceivedLetter(IModel channel, BasicDeliverEventArgs args, bool ackable) : base(channel, args, ackable)
        {
            Letter = JsonSerializer.Deserialize<Letter>(args.Body.Span);
        }
    }
}
