using System;
using System.IO;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utf8Json;

namespace CookedRabbit.Core
{
    public class ReceivedMessage : IDisposable
    {
        public bool Ackable { get; }
        private IModel Channel { get; set; }
        public byte[] Body { get; private set; }
        public ulong DeliveryTag { get; }
        public long Timestamp { get; }
        public string MessageId { get; }

        private TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();

        public ReceivedMessage(IModel channel, BasicGetResult result, bool ackable)
        {
            Ackable = ackable;
            Channel = channel;
            Body = result.Body.ToArray();
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
            Body = args.Body.ToArray();
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
