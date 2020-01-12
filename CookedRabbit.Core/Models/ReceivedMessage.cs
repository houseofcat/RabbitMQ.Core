using System.IO;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utf8Json;

namespace CookedRabbit.Core
{
    public class ReceivedMessage
    {
        public bool Ackable { get; }
        private IModel Channel { get; }
        public byte[] Body { get; }
        public ulong DeliveryTag { get; }
        public long Timestamp { get; }
        public string MessageId { get; }

        public ReceivedMessage() { }

        public ReceivedMessage(IModel channel, BasicGetResult result, bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            Body = result.Body;
            DeliveryTag = result.DeliveryTag;
            MessageId = result.BasicProperties.MessageId;

            if (result.BasicProperties.IsTimestampPresent())
            {
                Timestamp = result.BasicProperties.Timestamp.UnixTime;
            }
        }

        public ReceivedMessage(IModel channel, BasicDeliverEventArgs args, bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            Body = args.Body;
            DeliveryTag = args.DeliveryTag;
            MessageId = args.BasicProperties.MessageId;

            if (args.BasicProperties.IsTimestampPresent())
            {
                Timestamp = args.BasicProperties.Timestamp.UnixTime;
            }
        }

        /// <summary>
        /// Acknowledges the message server side.
        /// </summary>
        public bool AckMessage()
        {
            bool success = true;

            try { Channel.BasicAck(DeliveryTag, false); }
            catch { success = false; }

            return success;
        }

        /// <summary>
        /// Negative Acknowledges the message server side with option to requeue.
        /// </summary>
        public bool NackMessage(bool requeue)
        {
            bool success = true;

            try { Channel.BasicNack(DeliveryTag, false, requeue); }
            catch { success = false; }

            return success;
        }

        /// <summary>
        /// Reject Message server side with option to requeue.
        /// </summary>
        public bool RejectMessage(bool requeue)
        {
            bool success = true;

            try { Channel.BasicReject(DeliveryTag, requeue); }
            catch { success = false; }

            return success;
        }

        /// <summary>
        /// Convert internal Body to type <see cref="{T}" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T ConvertJsonBody<T>() => JsonSerializer.Deserialize<T>(Body);

        /// <summary>
        /// Convert internal Body to type <see cref="Letter" />.
        /// </summary>
        public Letter ConvertJsonBodyToLetter() => JsonSerializer.Deserialize<Letter>(Body);

        /// <summary>
        /// Convert internal Body as a Stream asynchronously to type <see cref="{T}" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public async Task<T> ConvertJsonBodyAsync<T>() =>
            await JsonSerializer
            .DeserializeAsync<T>(new MemoryStream(Body))
            .ConfigureAwait(false);

        /// <summary>
        /// Convert internal Body as a Stream asynchronously to type <see cref="Letter" />.
        /// </summary>
        public async Task<Letter> ConvertJsonBodyToLetterAsync() =>
            await JsonSerializer
            .DeserializeAsync<Letter>(new MemoryStream(Body))
            .ConfigureAwait(false);
    }
}
