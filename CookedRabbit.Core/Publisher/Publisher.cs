using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Pools;
using CookedRabbit.Core.Utils;
using Utf8Json;

namespace CookedRabbit.Core
{
    public class Publisher
    {
        public Config Config { get; }
        public ChannelPool ChannelPool { get; }

        public Channel<PublishReceipt> ReceiptBuffer { get; }

        public Publisher(Config config)
        {
            Guard.AgainstNull(config, nameof(config));

            Config = config;
            ChannelPool = new ChannelPool(Config);
            ReceiptBuffer = Channel.CreateUnbounded<PublishReceipt>();
        }

        public Publisher(ChannelPool channelPool)
        {
            Guard.AgainstNull(channelPool, nameof(channelPool));

            Config = channelPool.Config;
            ChannelPool = channelPool;
            ReceiptBuffer = Channel.CreateUnbounded<PublishReceipt>();
        }

        /// <summary>
        /// Acquires a channel from the channel pool, then publishes message based on the letter/envelope parameters.
        /// <para>Only throws exception when failing to acquire channel or when creating a receipt after the ReceiptBuffer is closed.</para>
        /// </summary>
        /// <param name="letter"></param>
        /// <param name="createReceipt"></param>
        public async Task PublishAsync(Letter letter, bool createReceipt)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            try
            {
                var props = chanHost.Channel.CreateBasicProperties();
                props.DeliveryMode = letter.Envelope.RoutingOptions?.DeliveryMode ?? 0;
                props.ContentType = letter.Envelope.RoutingOptions?.MessageType ?? string.Empty;
                props.Priority = letter.Envelope.RoutingOptions?.PriorityLevel ?? 0;

                chanHost.Channel.BasicPublish(
                    letter.Envelope.Exchange,
                    letter.Envelope.RoutingKey,
                    letter.Envelope.RoutingOptions?.Mandatory ?? false,
                    props,
                    JsonSerializer.Serialize(letter));
            }
            catch { error = true; }
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
        public async Task PublishManyAsync(IList<Letter> letters, bool createReceipt)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            for (int i = 0; i < letters.Count; i++)
            {
                try
                {
                    var props = chanHost.Channel.CreateBasicProperties();
                    props.DeliveryMode = letters[i].Envelope.RoutingOptions.DeliveryMode;
                    props.ContentType = letters[i].Envelope.RoutingOptions.MessageType;
                    props.Priority = letters[i].Envelope.RoutingOptions.PriorityLevel;

                    chanHost.Channel.BasicPublish(
                        letters[i].Envelope.Exchange,
                        letters[i].Envelope.RoutingKey,
                        letters[i].Envelope.RoutingOptions.Mandatory,
                        props,
                        JsonSerializer.Serialize(letters[i]));
                }
                catch
                { error = true; }

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
        public async Task PublishManyAsGroupAsync(IList<Letter> letters, bool createReceipt)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            try
            {
                var props = chanHost.Channel.CreateBasicProperties();
                props.DeliveryMode = letters[0].Envelope.RoutingOptions.DeliveryMode;
                props.ContentType = letters[0].Envelope.RoutingOptions.MessageType;
                props.Priority = letters[0].Envelope.RoutingOptions.PriorityLevel;

                var publishBatch = chanHost.Channel.CreateBasicPublishBatch();
                for (int i = 0; i < letters.Count; i++)
                {
                    publishBatch.Add(
                        letters[i].Envelope.Exchange,
                        letters[i].Envelope.RoutingKey,
                        letters[i].Envelope.RoutingOptions.Mandatory,
                        props,
                        JsonSerializer.Serialize(letters[i]));

                    if (createReceipt)
                    {
                        await CreateReceiptAsync(letters[i], error).ConfigureAwait(false);
                    }
                }

                publishBatch.Publish();
            }
            catch
            { error = true; }
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
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
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
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
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
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            return await ReceiptBuffer
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);
        }

#if CORE3
        /// <summary>
        /// Use this method to sequentially publish all messages of an IAsyncEnumerable (order is not 100% guaranteed).
        /// </summary>
        /// <param name="letters"></param>
        /// <param name="createReceipt"></param>
        public async Task PublishAsyncEnumerableAsync(IAsyncEnumerable<Letter> letters, bool createReceipt)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

            await foreach (var letter in letters)
            {
                try
                {
                    var props = chanHost.Channel.CreateBasicProperties();
                    props.DeliveryMode = letter.Envelope.RoutingOptions.DeliveryMode;
                    props.ContentType = letter.Envelope.RoutingOptions.MessageType;
                    props.Priority = letter.Envelope.RoutingOptions.PriorityLevel;

                    chanHost.Channel.BasicPublish(
                        letter.Envelope.Exchange,
                        letter.Envelope.RoutingKey,
                        letter.Envelope.RoutingOptions.Mandatory,
                        props,
                        letter.Body);
                }
                catch
                { error = true; }

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
                throw new InvalidOperationException(Strings.ChannelReadErrorMessage);
            }

            await foreach (var receipt in ReceiptBuffer.Reader.ReadAllAsync())
            {
                yield return receipt;
            }
        }
#endif
    }
}
