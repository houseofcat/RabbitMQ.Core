using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    internal sealed class BasicCancelOk : Work
    {
        private readonly string consumerTag;

        public BasicCancelOk(IBasicConsumer consumer, string consumerTag) : base(consumer)
        {
            this.consumerTag = consumerTag;
        }

        protected override async Task Execute(ModelBase model, IAsyncBasicConsumer consumer)
        {
            try
            {
                await consumer.HandleBasicCancelOk(consumerTag).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var details = new Dictionary<string, object>()
                {
                    {"consumer", consumer},
                    {"context",  "HandleBasicCancelOk"}
                };
                model.OnCallbackException(CallbackExceptionEventArgs.Build(e, details));
            }
        }
    }
}