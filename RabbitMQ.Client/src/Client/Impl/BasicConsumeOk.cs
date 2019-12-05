using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    internal sealed class BasicConsumeOk : Work
    {
        private readonly string consumerTag;

        public BasicConsumeOk(IBasicConsumer consumer, string consumerTag) : base(consumer)
        {
            this.consumerTag = consumerTag;
        }

        protected override async Task Execute(ModelBase model, IAsyncBasicConsumer consumer)
        {
            try
            {
                await consumer.HandleBasicConsumeOk(consumerTag).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var details = new Dictionary<string, object>()
                {
                    {"consumer", consumer},
                    {"context",  "HandleBasicConsumeOk"}
                };
                model.OnCallbackException(CallbackExceptionEventArgs.Build(e, details));
            }
        }
    }
}