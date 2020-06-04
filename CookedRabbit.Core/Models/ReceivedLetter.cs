using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace CookedRabbit.Core
{
    public class ReceivedLetter : ReceivedData
    {
        public ReceivedLetter(IModel channel, BasicGetResult result, bool ackable) : base(channel, result, ackable, null)
        {
            Letter = JsonSerializer.Deserialize<Letter>(result.Body.Span);
        }

        public ReceivedLetter(IModel channel, BasicDeliverEventArgs args, bool ackable) : base(channel, args, ackable, null)
        {
            Letter = JsonSerializer.Deserialize<Letter>(args.Body.Span);
        }
    }
}
