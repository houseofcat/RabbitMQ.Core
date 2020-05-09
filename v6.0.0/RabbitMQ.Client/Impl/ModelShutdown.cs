using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    internal sealed class ModelShutdown : Work
    {
        private readonly ShutdownEventArgs _reason;

        public ModelShutdown(IBasicConsumer consumer, ShutdownEventArgs reason) : base(consumer)
        {
            _reason = reason;
        }

        protected override async Task Execute(ModelBase model, IAsyncBasicConsumer consumer)
        {
            try
            {
                await consumer.HandleModelShutdown(model, _reason).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var details = new Dictionary<string, object>()
                {
                    { "consumer", consumer },
                    { "context", "HandleModelShutdown" }
                };
                model.OnCallbackException(CallbackExceptionEventArgs.Build(e, details));
            }
        }
    }
}
