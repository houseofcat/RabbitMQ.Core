using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CookedRabbit.Core
{
    public class ReceivedMessage : ReceivedData
    {
        public byte[] Body { get; private set; }

        private TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();

        public ReceivedMessage(IModel channel, BasicGetResult result, bool ackable) : base(channel, result, ackable)
        {
            Body = result.Body.ToArray();
        }

        public ReceivedMessage(IModel channel, BasicDeliverEventArgs args, bool ackable) : base(channel, args, ackable)
        {
            Body = args.Body.ToArray();
        }

        /// <summary>
        /// Convert internal Body to type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T ConvertJsonBody<T>() => JsonSerializer.Deserialize<T>(Body);

        /// <summary>
        /// Convert internal Body to type <see cref="Letter" />.
        /// </summary>
        public Letter ConvertJsonBodyToLetter() => JsonSerializer.Deserialize<Letter>(Body);

        /// <summary>
        /// Convert internal Body as a Stream asynchronously to type T.
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
