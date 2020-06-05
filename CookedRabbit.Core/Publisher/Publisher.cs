using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CookedRabbit.Core
{
    public interface IPublisher
    {
        IChannelPool ChannelPool { get; }
        Config Config { get; }
        Channel<PublishReceipt> ReceiptBuffer { get; }

        ValueTask<ChannelReader<PublishReceipt>> GetReceiptBufferReaderAsync();
        Task PublishAsync(Letter letter, bool createReceipt, bool withHeaders = true);
        Task<bool> PublishAsync(string exchangeName, string routingKey, ReadOnlyMemory<byte> payload, bool mandatory = false, IBasicProperties messageProperties = null);
        Task<bool> PublishAsync(string exchangeName, string routingKey, ReadOnlyMemory<byte> payload, IDictionary<string, object> headers = null, byte? priority = 0, bool mandatory = false);
        Task PublishAsyncEnumerableAsync(IAsyncEnumerable<Letter> letters, bool createReceipt, bool withHeaders = true);
        Task<bool> PublishBatchAsync(string exchangeName, string routingKey, IList<ReadOnlyMemory<byte>> payloads, bool mandatory = false, IBasicProperties messageProperties = null);
        Task<bool> PublishBatchAsync(string exchangeName, string routingKey, IList<ReadOnlyMemory<byte>> payloads, IDictionary<string, object> headers = null, byte? priority = 0, bool mandatory = false);
        Task PublishManyAsGroupAsync(IList<Letter> letters, bool createReceipt, bool withHeaders = true);
        Task PublishManyAsync(IList<Letter> letters, bool createReceipt, bool withHeaders = true);
        IAsyncEnumerable<PublishReceipt> ReadAllPublishReceiptsAsync();
        ValueTask<PublishReceipt> ReadPublishReceiptAsync();
    }

    public class Publisher : IPublisher
    {
        private ILogger<Publisher> _logger;

        public Config Config { get; }
        public IChannelPool ChannelPool { get; }

        public Channel<PublishReceipt> ReceiptBuffer { get; }

        public Publisher(Config config)
        {
            Guard.AgainstNull(config, nameof(config));

            _logger = LogHelper.GetLogger<Publisher>();
            Config = config;
            ChannelPool = new ChannelPool(Config);
            ReceiptBuffer = Channel.CreateUnbounded<PublishReceipt>();
        }

        public Publisher(IChannelPool channelPool)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));

            _logger = LogHelper.GetLogger<Publisher>();
            Config = channelPool.Config;
            ChannelPool = channelPool;
            ReceiptBuffer = Channel.CreateUnbounded<PublishReceipt>();
        }

        // A basic implementation of publish but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
        public async Task<bool> PublishAsync(
            string exchangeName,
            string routingKey,
            ReadOnlyMemory<byte> payload,
            bool mandatory = false,
            IBasicProperties messageProperties = null)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

            var error = false;
            var channelHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);
            if (messageProperties == null)
            {
                messageProperties = channelHost.Channel.CreateBasicProperties();
                messageProperties.DeliveryMode = 2;

                if (!messageProperties.IsHeadersPresent())
                {
                    messageProperties.Headers = new Dictionary<string, object>();
                }
            }

            // Non-optional Header.
            messageProperties.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

            try
            {
                channelHost.Channel.BasicPublish(
                    exchange: exchangeName ?? string.Empty,
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: messageProperties,
                    body: payload);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(LogMessages.Publisher.PublishFailed, $"{exchangeName}->{routingKey}", ex.Message);
                error = true;
            }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(channelHost, error);
            }

            return error;
        }

        // A basic implementation of publish but using the ChannelPool. If headers are provided and start with "x-", they get included in the message properties.
        public async Task<bool> PublishAsync(
            string exchangeName,
            string routingKey,
            ReadOnlyMemory<byte> payload,
            IDictionary<string, object> headers = null,
            byte? priority = 0,
            bool mandatory = false)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));

            var error = false;
            var channelHost = await ChannelPool.GetChannelAsync().ConfigureAwait(false);

            try
            {
                channelHost.Channel.BasicPublish(
                    exchange: exchangeName ?? string.Empty,
                    routingKey: routingKey,
                    mandatory: mandatory,
                    basicProperties: BuildProperties(headers, channelHost, priority),
                    body: payload);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    LogMessages.Publisher.PublishFailed,
                    $"{exchangeName}->{routingKey}",
                    ex.Message);

                error = true;
            }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(channelHost, error);
            }

            return error;
        }

        // A basic implementation of publishing batches but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
        public async Task<bool> PublishBatchAsync(
            string exchangeName,
            string routingKey,
            IList<ReadOnlyMemory<byte>> payloads,
            bool mandatory = false,
            IBasicProperties messageProperties = null)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));
            Guard.AgainstNullOrEmpty(payloads, nameof(payloads));

            var error = false;
            var channelHost = await ChannelPool.GetChannelAsync();
            if (messageProperties == null)
            {
                messageProperties = channelHost.Channel.CreateBasicProperties();
                messageProperties.DeliveryMode = 2;

                if (!messageProperties.IsHeadersPresent())
                {
                    messageProperties.Headers = new Dictionary<string, object>();
                }
            }

            // Non-optional Header.
            messageProperties.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

            try
            {
                var batch = channelHost.Channel.CreateBasicPublishBatch();

                for (int i = 0; i < payloads.Count; i++)
                {
                    batch.Add(exchangeName, routingKey, mandatory, messageProperties, payloads[i]);
                }

                batch.Publish();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    LogMessages.Publisher.PublishFailed,
                    $"{exchangeName}->{routingKey}",
                    ex.Message);

                error = true;
            }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(channelHost, error);
            }

            return error;
        }


        // A basic implementation of publishing batches but using the ChannelPool. If message properties is null, one is created and all messages are set to persistent.
        public async Task<bool> PublishBatchAsync(
            string exchangeName,
            string routingKey,
            IList<ReadOnlyMemory<byte>> payloads,
            IDictionary<string, object> headers = null,
            byte? priority = 0,
            bool mandatory = false)
        {
            Guard.AgainstBothNullOrEmpty(exchangeName, nameof(exchangeName), routingKey, nameof(routingKey));
            Guard.AgainstNullOrEmpty(payloads, nameof(payloads));

            var error = false;
            var channelHost = await ChannelPool.GetChannelAsync();

            try
            {
                var batch = channelHost.Channel.CreateBasicPublishBatch();

                for (int i = 0; i < payloads.Count; i++)
                {
                    batch.Add(exchangeName, routingKey, mandatory, BuildProperties(headers, channelHost, priority), payloads[i]);
                }

                batch.Publish();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    LogMessages.Publisher.PublishFailed,
                    $"{exchangeName}->{routingKey}",
                    ex.Message);

                error = true;
            }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(channelHost, error);
            }

            return error;
        }

        /// <summary>
        /// Acquires a channel from the channel pool, then publishes message based on the letter/envelope parameters.
        /// <para>Only throws exception when failing to acquire channel or when creating a receipt after the ReceiptBuffer is closed.</para>
        /// </summary>
        /// <param name="letter"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withHeaders"></param>
        public async Task PublishAsync(Letter letter, bool createReceipt, bool withHeaders = true)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            try
            {
                chanHost.Channel.BasicPublish(
                    letter.Envelope.Exchange,
                    letter.Envelope.RoutingKey,
                    letter.Envelope.RoutingOptions?.Mandatory ?? false,
                    BuildProperties(letter, chanHost, withHeaders),
                    JsonSerializer.SerializeToUtf8Bytes(letter));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    LogMessages.Publisher.PublishLetterFailed,
                    $"{letter.Envelope.Exchange}->{letter.Envelope.RoutingKey}",
                    letter.LetterId,
                    ex.Message);

                error = true;
            }
            finally
            {
                if (createReceipt)
                {
                    await CreateReceiptAsync(letter, error)
                        .ConfigureAwait(false);
                }

                await ChannelPool
                    .ReturnChannelAsync(chanHost, error);
            }
        }

        /// <summary>
        /// Use this method to sequentially publish all messages in a list in the order received.
        /// </summary>
        /// <param name="letters"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withHeaders"></param>
        public async Task PublishManyAsync(IList<Letter> letters, bool createReceipt, bool withHeaders = true)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            for (int i = 0; i < letters.Count; i++)
            {
                try
                {
                    chanHost.Channel.BasicPublish(
                        letters[i].Envelope.Exchange,
                        letters[i].Envelope.RoutingKey,
                        letters[i].Envelope.RoutingOptions.Mandatory,
                        BuildProperties(letters[i], chanHost, withHeaders),
                        JsonSerializer.SerializeToUtf8Bytes(letters[i]));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(
                        LogMessages.Publisher.PublishLetterFailed,
                        $"{letters[i].Envelope.Exchange}->{letters[i].Envelope.RoutingKey}",
                        letters[i].LetterId,
                        ex.Message);

                    error = true;
                }

                if (createReceipt)
                { await CreateReceiptAsync(letters[i], error).ConfigureAwait(false); }

                if (error) { break; }
            }

            await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false);
        }

        /// <summary>
        /// Use this method when a group of letters who have the same properties (deliverymode, messagetype, priority).
        /// <para>Receipt with no error indicates that we successfully handed off to internal library, not necessarily published.</para>
        /// </summary>
        /// <param name="letters"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withHeaders"></param>
        public async Task PublishManyAsGroupAsync(IList<Letter> letters, bool createReceipt, bool withHeaders = true)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            try
            {
                if (letters.Count > 0)
                {
                    var publishBatch = chanHost.Channel.CreateBasicPublishBatch();
                    for (int i = 0; i < letters.Count; i++)
                    {
                        publishBatch.Add(
                            letters[i].Envelope.Exchange,
                            letters[i].Envelope.RoutingKey,
                            letters[i].Envelope.RoutingOptions.Mandatory,
                            BuildProperties(letters[i], chanHost, withHeaders),
                            JsonSerializer.SerializeToUtf8Bytes(letters[i]));

                        if (createReceipt)
                        {
                            await CreateReceiptAsync(letters[i], error).ConfigureAwait(false);
                        }
                    }

                    publishBatch.Publish();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    LogMessages.Publisher.PublishBatchFailed,
                    ex.Message);

                error = true;
            }
            finally
            { await ChannelPool.ReturnChannelAsync(chanHost, error).ConfigureAwait(false); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask CreateReceiptAsync(Letter letter, bool error)
        {
            if (!await ReceiptBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            await ReceiptBuffer
                .Writer
                .WriteAsync(new PublishReceipt { LetterId = letter.LetterId, IsError = error, OriginalLetter = error ? letter : null })
                .ConfigureAwait(false);
        }

        public async ValueTask<ChannelReader<PublishReceipt>> GetReceiptBufferReaderAsync()
        {
            if (!await ReceiptBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            return ReceiptBuffer.Reader;
        }

        public async ValueTask<PublishReceipt> ReadPublishReceiptAsync()
        {
            if (!await ReceiptBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            return await ReceiptBuffer
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Use this method to sequentially publish all messages of an IAsyncEnumerable (order is not 100% guaranteed).
        /// </summary>
        /// <param name="letters"></param>
        /// <param name="createReceipt"></param>
        /// <param name="withHeaders"></param>
        public async Task PublishAsyncEnumerableAsync(IAsyncEnumerable<Letter> letters, bool createReceipt, bool withHeaders = true)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            await foreach (var letter in letters)
            {
                try
                {
                    chanHost.Channel.BasicPublish(
                        letter.Envelope.Exchange,
                        letter.Envelope.RoutingKey,
                        letter.Envelope.RoutingOptions.Mandatory,
                        BuildProperties(letter, chanHost, withHeaders),
                        letter.Body);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(
                        LogMessages.Publisher.PublishFailed,
                        $"{letter.Envelope.Exchange}->{letter.Envelope.RoutingKey}",
                        ex.Message);

                    error = true;
                }

                if (createReceipt) { await CreateReceiptAsync(letter, error).ConfigureAwait(false); }

                if (error) { break; }
            }

            await ChannelPool.ReturnChannelAsync(chanHost, error);
        }

        public async IAsyncEnumerable<PublishReceipt> ReadAllPublishReceiptsAsync()
        {
            if (!await ReceiptBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelReadErrorMessage);
            }

            await foreach (var receipt in ReceiptBuffer.Reader.ReadAllAsync())
            {
                yield return receipt;
            }
        }

        private IBasicProperties BuildProperties(Letter letter, IChannelHost channelHost, bool withHeaders)
        {
            var props = channelHost.Channel.CreateBasicProperties();

            props.DeliveryMode = letter.Envelope.RoutingOptions.DeliveryMode;
            props.ContentType = letter.Envelope.RoutingOptions.MessageType;
            props.Priority = letter.Envelope.RoutingOptions.PriorityLevel;

            if (!props.IsHeadersPresent())
            {
                props.Headers = new Dictionary<string, object>();
            }

            if (withHeaders && letter.LetterMetadata != null)
            {
                foreach (var kvp in letter.LetterMetadata?.CustomFields)
                {
                    if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        props.Headers[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Non-optional Header.
            props.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForLetter;

            return props;
        }

        private IBasicProperties BuildProperties(IDictionary<string, object> headers, IChannelHost channelHost, byte? priority = 0, byte? deliveryMode = 2)
        {
            var props = channelHost.Channel.CreateBasicProperties();
            props.DeliveryMode = deliveryMode ?? 2; // Default Persisted
            props.Priority = priority ?? 0; // Default Priority

            if (!props.IsHeadersPresent())
            {
                props.Headers = new Dictionary<string, object>();
            }

            if (headers != null && headers.Count > 0)
            {
                foreach (var kvp in headers)
                {
                    if (kvp.Key.StartsWith(Constants.HeaderPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        props.Headers[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Non-optional Header.
            props.Headers[Constants.HeaderForObjectType] = Constants.HeaderValueForMessage;

            return props;
        }
    }
}
