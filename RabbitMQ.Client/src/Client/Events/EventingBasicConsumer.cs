using System;

namespace RabbitMQ.Client.Events
{
    ///<summary>Experimental class exposing an IBasicConsumer's
    ///methods as separate events.</summary>
    public class EventingBasicConsumer : DefaultBasicConsumer
    {
        ///<summary>Constructor which sets the Model property to the
        ///given value.</summary>
        public EventingBasicConsumer(IModel model) : base(model)
        {
        }

        ///<summary>Event fired on HandleBasicDeliver.</summary>
        public event EventHandler<BasicDeliverEventArgs> Received;

        ///<summary>Event fired on HandleBasicConsumeOk.</summary>
        public event EventHandler<ConsumerEventArgs> Registered;

        ///<summary>Event fired on HandleModelShutdown.</summary>
        public event EventHandler<ShutdownEventArgs> Shutdown;

        ///<summary>Event fired on HandleBasicCancelOk.</summary>
        public event EventHandler<ConsumerEventArgs> Unregistered;

        ///<summary>Fires the Unregistered event.</summary>
        public override void HandleBasicCancelOk(string consumerTag)
        {
            base.HandleBasicCancelOk(consumerTag);
            Raise(Unregistered, new ConsumerEventArgs(consumerTag));
        }

        ///<summary>Fires the Registered event.</summary>
        public override void HandleBasicConsumeOk(string consumerTag)
        {
            base.HandleBasicConsumeOk(consumerTag);
            Raise(Registered, new ConsumerEventArgs(consumerTag));
        }

        ///<summary>Fires the Received event.</summary>
        public override void HandleBasicDeliver(string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            byte[] body)
        {
            base.HandleBasicDeliver(consumerTag,
                deliveryTag,
                redelivered,
                exchange,
                routingKey,
                properties,
                body);
            Raise(Received, new BasicDeliverEventArgs(consumerTag,
                                            deliveryTag,
                                            redelivered,
                                            exchange,
                                            routingKey,
                                            properties,
                                            body));
        }

        ///<summary>Fires the Shutdown event.</summary>
        public override void HandleModelShutdown(object model, ShutdownEventArgs reason)
        {
            base.HandleModelShutdown(model, reason);
            Raise(Shutdown, reason);
        }

        private void Raise<TEvent>(EventHandler<TEvent> eventHandler, TEvent evt)
        {
            var handler = eventHandler;
            if (handler != null)
            {
                handler(this, evt);
            }
        }
    }
}
