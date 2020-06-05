using CookedRabbit.Core.Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CookedRabbit.Core
{
    public interface IReceivedData
    {
        bool Ackable { get; }
        IModel Channel { get; set; }
        string ContentType { get; }
        byte[] Data { get; }
        ulong DeliveryTag { get; }
        Letter Letter { get; }
        IBasicProperties Properties { get; }

        bool AckMessage();
        void Complete();
        Task<bool> Completion();
        void Dispose();
        ReadOnlyMemory<byte> GetBody();
        string GetBodyAsUtf8String();
        Task<TResult> GetTypeFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null);
        Task<IEnumerable<TResult>> GetTypesFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null);
        bool NackMessage(bool requeue);
        void ReadHeaders();
        bool RejectMessage(bool requeue);
    }

    public class ReceivedData : IDisposable, IReceivedData
    {
        public IBasicProperties Properties { get; }
        public bool Ackable { get; }
        public IModel Channel { get; set; }
        public ulong DeliveryTag { get; }
        public byte[] Data { get; }
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
            Data = result.Body.ToArray();
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
            Data = args.Body.ToArray();
            _hashKey = hashKey;

            ReadHeaders();
        }

        public void ReadHeaders()
        {
            if (Properties?.Headers != null && Properties.Headers.ContainsKey(Constants.HeaderForObjectType))
            {
                ContentType = Encoding.UTF8.GetString((byte[])Properties.Headers[Constants.HeaderForObjectType]);
            }
            else
            {
                ContentType = Constants.HeaderValueForUnknown;
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
        /// <em>Note: Does not decomcrypt Letter body.</em>
        /// </summary>
        /// <returns></returns>
        public ReadOnlyMemory<byte> GetBody()
        {
            switch (ContentType)
            {
                case Constants.HeaderValueForLetter:

                    if (Letter == null)
                    { Letter = JsonSerializer.Deserialize<Letter>(Data); }

                    return Letter.Body;

                case Constants.HeaderValueForMessage:
                default:

                    return Data;
            }
        }

        /// <summary>
        /// Use this method to retrieve the internal buffer as string.
        /// <para>Combine this with AMQP header X-CR-OBJECTTYPE to get message wrapper payloads.</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "LETTER")</para>
        /// <para>Header Example: ("X-CR-OBJECTTYPE", "MESSAGE")</para>
        /// <em>Note: Does not decomcrypt Letter body.</em>
        /// </summary>
        /// <returns></returns>
        public string GetBodyAsUtf8String()
        {
            switch (ContentType)
            {
                case Constants.HeaderValueForLetter:

                    if (Letter == null)
                    { Letter = JsonSerializer.Deserialize<Letter>(Data); }

                    return Encoding.UTF8.GetString(Letter.Body);

                case Constants.HeaderValueForMessage:
                default:

                    return Encoding.UTF8.GetString(Data);
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
                case Constants.HeaderValueForLetter:

                    if (Letter == null)
                    { Letter = JsonSerializer.Deserialize<Letter>(Data); }

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

                    return JsonSerializer.Deserialize<TResult>(Letter.Body, jsonSerializerOptions);

                case Constants.HeaderValueForMessage:
                default:

                    if (Bytes.IsJson(Data))
                    {
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

                        return JsonSerializer.Deserialize<TResult>(data ?? Data, jsonSerializerOptions);
                    }
                    else
                    { return default(TResult); }
            }
        }

        /// <summary>
        /// Use this method to attempt to deserialize into your types based on internal buffer.
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
        public async Task<IEnumerable<TResult>> GetTypesFromJsonAsync<TResult>(bool decrypt = false, bool decompress = false, JsonSerializerOptions jsonSerializerOptions = null)
        {
            switch (ContentType)
            {
                case Constants.HeaderValueForLetter:

                    var types = new List<TResult>();
                    if (Letter == null)
                    { Letter = JsonSerializer.Deserialize<Letter>(Data); }

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

                    return JsonSerializer.Deserialize<List<TResult>>(Letter.Body.AsSpan(), jsonSerializerOptions);

                case Constants.HeaderValueForMessage:
                default:

                    if (Bytes.IsJsonArray(Data))
                    {
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

                        return JsonSerializer.Deserialize<List<TResult>>(data ?? Data, jsonSerializerOptions);
                    }
                    else
                    { return default(List<TResult>); }

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
