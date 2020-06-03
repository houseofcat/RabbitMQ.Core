using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CookedRabbit.Core.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CookedRabbit.Core
{
    public interface IReceivedData
    {
        IBasicProperties Properties { get; }
        bool Ackable { get; }
        ulong DeliveryTag { get; }
        ReadOnlyMemory<byte> Data { get; }
        Letter Letter { get; }
        string ContentType { get; }

        /// <summary>
        /// Use this method to retrieve the internal buffer as byte[].
        /// <para>Combine this with header X-CR-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Does not decomprypt Letter body.</em>
        /// </summary>
        /// <returns></returns>
        ReadOnlyMemory<byte> GetBody();

        /// <summary>
        /// Use this method to attempt to deserialize into your type based on internal buffer.
        /// <para>Combine this with AMQP header X-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Always decomprypts Letter body regardless of parameters.</em>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="decrypt">Is only used for Message/Unknown types.</param>
        /// <param name="decompress">Is only used on Message/Unknown types.</param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        Task<TResult> GetTypeFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null);

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
        public ReadOnlyMemory<byte> Data { get; }
        public Letter Letter { get; protected set; }
        private ReadOnlyMemory<byte> _hashKey { get; }
        public string ContentType { get; private set; }

        private TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();

        public ReceivedData(
            IModel channel,
            BasicGetResult result,
            bool ackable,
            ReadOnlyMemory<byte> hashKey)
        {
            Ackable = ackable;
            Channel = channel;
            DeliveryTag = result.DeliveryTag;
            Properties = result.BasicProperties;
            Data = result.Body;
            _hashKey = hashKey;

            ReadHeaders();
        }

        public ReceivedData(
            IModel channel,
            BasicDeliverEventArgs args,
            bool ackable,
            ReadOnlyMemory<byte> hashKey)
        {
            Ackable = ackable;
            Channel = channel;
            DeliveryTag = args.DeliveryTag;
            Properties = args.BasicProperties;
            Data = args.Body;
            _hashKey = hashKey;

            ReadHeaders();
        }

        public void ReadHeaders()
        {
            if (Properties.Headers.ContainsKey(Strings.HeaderForObjectType))
            {
                ContentType = Encoding.UTF8.GetString((byte[])Properties.Headers[Strings.HeaderForObjectType]);
            }
            else
            {
                ContentType = Strings.HeaderValueForUnknown;
            }
        }

        /// <summary>
        /// Acknowledges the message server side.
        /// </summary>
        public bool AckMessage()
        {
            var success = true;

            if (Ackable)
            {
                try
                {
                    Channel?.BasicAck(DeliveryTag, false);
                    Channel = null;
                }
                catch { success = false; }
            }

            return success;
        }

        /// <summary>
        /// Negative Acknowledges the message server side with option to requeue.
        /// </summary>
        public bool NackMessage(bool requeue)
        {
            var success = true;

            if (Ackable)
            {
                try
                {
                    Channel?.BasicNack(DeliveryTag, false, requeue);
                    Channel = null;
                }
                catch { success = false; }
            }

            return success;
        }

        /// <summary>
        /// Reject Message server side with option to requeue.
        /// </summary>
        public bool RejectMessage(bool requeue)
        {
            var success = true;

            if (Ackable)
            {
                try
                {
                    Channel?.BasicReject(DeliveryTag, requeue);
                    Channel = null;
                }
                catch { success = false; }
            }

            return success;
        }


        /// <summary>
        /// Use this method to retrieve the internal buffer as byte[].
        /// <para>Combine this with AMQP header X-CR-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Always decomprypts Letter body regardless of parameters.</em>
        /// </summary>
        /// <returns></returns>
        public ReadOnlyMemory<byte> GetBody()
        {
            switch (ContentType)
            {
                case Strings.HeaderValueForLetter:

                    if (Letter == null)
                    { Letter = JsonSerializer.Deserialize<Letter>(Data.Span); }

                    return Letter.Body;

                case Strings.HeaderValueForMessage:
                default:

                    return Data;
            }
        }

        /// <summary>
        /// Use this method to attempt to deserialize into your type based on internal buffer.
        /// <para>Combine this with AMQP header X-CR-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Always decomprypts Letter bodies to get type regardless of parameters.</em>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="decrypt"></param>
        /// <param name="decompress"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public async Task<TResult> GetTypeFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null)
        {
            switch (ContentType)
            {
                case Strings.HeaderValueForLetter:

                    if (Letter == null)
                    { Letter = JsonSerializer.Deserialize<Letter>(Data.Span); }

                    if (Letter.LetterMetadata.Encrypted && _hashKey.Length > 0)
                    {
                        Letter.Body = AesEncrypt.Decrypt(Letter.Body, _hashKey);
                        Letter.LetterMetadata.Encrypted = false;
                    }

                    if (Letter.LetterMetadata.Compressed)
                    {
                        Letter.Body = await Gzip
                            .DecompressAsync(Letter.Body)
                            .ConfigureAwait(false);

                        Letter.LetterMetadata.Compressed = false;
                    }

                    return JsonSerializer.Deserialize<TResult>(Letter.Body.AsSpan(), jsonSerializerOptions);

                case Strings.HeaderValueForMessage:
                default:

                    byte[] data = null;
                    if (decrypt && _hashKey.Length > 0)
                    {
                        data = AesEncrypt.Decrypt(Data, _hashKey);
                    }

                    if (decompress)
                    {
                        data = await Gzip
                            .DecompressAsync(data ?? Data)
                            .ConfigureAwait(false);
                    }

                    return JsonSerializer.Deserialize<TResult>(data ?? Data.Span, jsonSerializerOptions);
            }
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
            if (Letter != null) { Letter = null; }

            CompletionSource.Task.Dispose();
        }
    }
}
