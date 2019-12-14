using System;
using System.Runtime.CompilerServices;
#if CORE3
using System.Collections.Generic;
#endif
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Configs;
using CookedRabbit.Core.Pools;

namespace CookedRabbit.Core.Publisher
{
    public class Publisher
    {
        private Config Config { get; }
        private ChannelPool ChannelPool { get; }

        private Channel<PublishReceipt> ReceiptBuffer { get; }

        private const string ChannelError = "Can't use reader on a closed Threading.Channel.";

        public Publisher(Config config)
        {
            Config = config;
        }

        public Publisher(ChannelPool channelPool)
        {
            ChannelPool = channelPool;
            Config = ChannelPool.Config;
            ReceiptBuffer = Channel.CreateUnbounded<PublishReceipt>();
        }

        public async ValueTask PublishAsync(Letter letter)
        {
            var error = false;
            var chanHost = await ChannelPool
                .GetChannelAsync()
                .ConfigureAwait(false);

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
            {
                error = true;
                throw;
            }
            finally
            {
                await ChannelPool
                    .ReturnChannelAsync(chanHost, error);
            }
        }

        public async ValueTask PublishWithReceiptAsync(Letter letter)
        {
            ChannelHost chanHost;
            var error = false;
            try
            {
                chanHost = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);
            }
            catch
            { error = true; }

            if (error)
            {
                await CreateReceiptAsync(letter.LetterId, error);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask CreateReceiptAsync(ulong letterId, bool error)
        {
            if (!await ReceiptBuffer
                .Writer
                .WaitToWriteAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ChannelError);
            }

            await ReceiptBuffer
                .Writer
                .WriteAsync(new PublishReceipt { LetterId = letterId, IsError = error })
                .ConfigureAwait(false);
        }

        public async ValueTask<ChannelReader<PublishReceipt>> GetReceiptsReader()
        {
            if (!await ReceiptBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ChannelError);
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
                throw new InvalidOperationException(ChannelError);
            }

            return await ReceiptBuffer.Reader.ReadAsync().ConfigureAwait(false);
        }

#if CORE3
        public async IAsyncEnumerable<PublishReceipt> ReadAllPublishReceiptsAsync()
        {
            if (!await ReceiptBuffer
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ChannelError);
            }

            await foreach (var receipt in ReceiptBuffer.Reader.ReadAllAsync())
            {
                yield return receipt;
            }
        }
#endif
    }
}
