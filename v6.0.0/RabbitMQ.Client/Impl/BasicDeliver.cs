using RabbitMQ.Client.Events;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    public sealed class BasicDeliver : Work
    {
        private readonly string _consumerTag;
        private readonly ulong _deliveryTag;
        private readonly bool _redelivered;
        private readonly string _exchange;
        private readonly string _routingKey;
        private readonly IBasicProperties _basicProperties;
        private readonly IMemoryOwner<byte> _body;
        private readonly int _bodyLength;

        public BasicDeliver(IBasicConsumer consumer,
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties basicProperties,
            IMemoryOwner<byte> body,
            int bodyLength) : base(consumer)
        {
            _consumerTag = consumerTag;
            _deliveryTag = deliveryTag;
            _redelivered = redelivered;
            _exchange = exchange;
            _routingKey = routingKey;
            _basicProperties = basicProperties;
            _body = body;
            _bodyLength = bodyLength;
        }

        protected override async Task Execute(ModelBase model, IAsyncBasicConsumer consumer)
        {
            try
            {
                await consumer.HandleBasicDeliver(_consumerTag,
                    _deliveryTag,
                    _redelivered,
                    _exchange,
                    _routingKey,
                    _basicProperties,
                    _body.Memory.Slice(0, _bodyLength)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var details = new Dictionary<string, object>()
                {
                    {"consumer", consumer},
                    {"context",  "HandleBasicDeliver"}
                };
                model.OnCallbackException(CallbackExceptionEventArgs.Build(e, details));
            }
            finally
            {
                _body.Dispose();
            }
        }
    }
}
