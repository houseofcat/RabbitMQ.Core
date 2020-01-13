using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utf8Json;

namespace CookedRabbit.Core
{
    public class ReceivedLetter : IDisposable
    {
        public bool Ackable { get; }
        private IModel Channel { get; set; }
        public Letter Letter { get; private set; }
        public ulong DeliveryTag { get; }
        public long Timestamp { get; }
        public string MessageId { get; }

        public ReceivedLetter(IModel channel, BasicGetResult result, bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            Letter = result.Body != null ? JsonSerializer.Deserialize<Letter>(result.Body) : null;
            DeliveryTag = result.DeliveryTag;
            MessageId = result.BasicProperties.MessageId;

            if (result.BasicProperties.IsTimestampPresent())
            {
                Timestamp = result.BasicProperties.Timestamp.UnixTime;
            }
        }

        public ReceivedLetter(IModel channel, BasicDeliverEventArgs args, bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            Letter = args.Body != null ? JsonSerializer.Deserialize<Letter>(args.Body) : null;
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

            try
            {
                Channel?.BasicAck(DeliveryTag, false);
                Channel = null;
            }
            catch { success = false; }

            return success;
        }

        /// <summary>
        /// Negative Acknowledges the message server side with option to requeue.
        /// </summary>
        public bool NackMessage(bool requeue)
        {
            bool success = true;

            try
            {
                Channel?.BasicNack(DeliveryTag, false, requeue);
                Channel = null;
            }
            catch { success = false; }

            return success;
        }

        /// <summary>
        /// Reject Message server side with option to requeue.
        /// </summary>
        public bool RejectMessage(bool requeue)
        {
            bool success = true;

            try
            {
                Channel?.BasicReject(DeliveryTag, requeue);
                Channel = null;
            }
            catch { success = false; }

            return success;
        }

        public void Dispose()
        {
            if (Channel != null) { Channel = null; }
            if (Letter != null) { Letter = null; }
        }
    }
}
