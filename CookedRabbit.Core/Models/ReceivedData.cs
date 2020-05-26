using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CookedRabbit.Core
{
    public interface IReceivedData
    {
        IBasicProperties Properties { get; }
        bool Ackable { get; }
        ulong DeliveryTag { get; }

        bool AckMessage();
        void Complete();
        Task<bool> Completion();
        bool NackMessage(bool requeue);
        bool RejectMessage(bool requeue);
    }

    public class ReceivedData : IReceivedData, IDisposable
    {
        public IBasicProperties Properties { get; }
        public bool Ackable { get; }
        public IModel Channel { get; set; }
        public ulong DeliveryTag { get; }

        private TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();

        public ReceivedData(IModel channel, BasicGetResult result, bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            DeliveryTag = result.DeliveryTag;
            Properties = result.BasicProperties;
        }

        public ReceivedData(IModel channel, BasicDeliverEventArgs args, bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            DeliveryTag = args.DeliveryTag;
            Properties = args.BasicProperties;
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
        /// A way to indicate this message is fully finished with.
        /// </summary>
        public void Complete() => CompletionSource.SetResult(true);

        /// <summary>
        /// A way to await the message until it is marked complete.
        /// </summary>
        public Task<bool> Completion() => CompletionSource.Task;

        public void Dispose()
        {
            if (Channel != null) { Channel = null; }

            CompletionSource.Task.Dispose();
        }
    }
}
