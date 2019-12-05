using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    internal sealed class BasicDeliver : Work
    {
        private readonly string consumerTag;
        private readonly ulong deliveryTag;
        private readonly bool redelivered;
        private readonly string exchange;
        private readonly string routingKey;
        private readonly IBasicProperties basicProperties;
        private readonly byte[] body;

        public BasicDeliver(IBasicConsumer consumer,
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties basicProperties,
            byte[] body) : base(consumer)
        {
            this.consumerTag = consumerTag;
            this.deliveryTag = deliveryTag;
            this.redelivered = redelivered;
            this.exchange = exchange;
            this.routingKey = routingKey;
            this.basicProperties = basicProperties;
            this.body = body;
        }

        protected override async Task Execute(ModelBase model, IAsyncBasicConsumer consumer)
        {
            try
            {
                await consumer.HandleBasicDeliver(consumerTag,
                    deliveryTag,
                    redelivered,
                    exchange,
                    routingKey,
                    basicProperties,
                    body).ConfigureAwait(false);
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
        }
    }
}