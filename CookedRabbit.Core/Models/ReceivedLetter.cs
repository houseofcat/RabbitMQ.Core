using System;
using System.Threading.Tasks;
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
        private TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();

        public ReceivedLetter(IModel channel, BasicGetResult result, bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            Letter = JsonSerializer.Deserialize<Letter>(result.Body.ToArray());
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
            Letter = JsonSerializer.Deserialize<Letter>(args.Body.ToArray());
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

        /// <summary>
        /// A way to indicate this letter is fully finished with.
        /// </summary>
        public void Complete() => CompletionSource.SetResult(true);

        /// <summary>
        /// A way to await the letter until it is marked complete.
        /// </summary>
        public Task<bool> Completion() => CompletionSource.Task;

        public void Dispose()
        {
            if (Channel != null) { Channel = null; }
            if (Letter != null) { Letter = null; }

            CompletionSource.Task.Dispose();
        }
    }
}
