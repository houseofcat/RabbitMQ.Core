using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CookedRabbit.Core
{
    public class ReceivedMessage : ReceivedData
    {
        public ReceivedMessage(IModel channel, BasicGetResult result, bool ackable) : base(channel, result, ackable, null)
        { }

        public ReceivedMessage(IModel channel, BasicDeliverEventArgs args, bool ackable) : base(channel, args, ackable, null)
        { }

        /// <summary>
        /// Convert internal Body to type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T ConvertJsonBody<T>(JsonSerializerOptions options = null) => JsonSerializer.Deserialize<T>(Data.Span, options);

        /// <summary>
        /// Convert internal Body to type <see cref="Letter" />.
        /// </summary>
        public Letter ConvertJsonBodyToLetter() => JsonSerializer.Deserialize<Letter>(Data.Span);

        /// <summary>
        /// Convert internal Body as a Stream asynchronously to type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public async Task<T> ConvertJsonBodyAsync<T>(JsonSerializerOptions options = null) =>
            await JsonSerializer
            .DeserializeAsync<T>(new MemoryStream(Data.ToArray()), options)
            .ConfigureAwait(false);

        /// <summary>
        /// Convert internal Body as a Stream asynchronously to type <see cref="Letter" />.
        /// </summary>
        public async Task<Letter> ConvertJsonBodyToLetterAsync() =>
            await JsonSerializer
            .DeserializeAsync<Letter>(new MemoryStream(Data.ToArray()))
            .ConfigureAwait(false);
    }
}
