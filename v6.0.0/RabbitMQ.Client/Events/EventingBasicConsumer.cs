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

        ///<summary>
        /// Event fired when a delivery arrives for the consumer.
        /// </summary>
        /// <remarks>
        /// Handlers must copy or fully use delivery body before returning.
        /// Accessing the body at a later point is unsafe as its memory can
        /// be already released.
        /// </remarks>
        public event EventHandler<BasicDeliverEventArgs> Received;

        ///<summary>Fires when the server confirms successful consumer cancelation.</summary>
        public event EventHandler<ConsumerEventArgs> Registered;

        ///<summary>Fires on model (channel) shutdown, both client and server initiated.</summary>
        public event EventHandler<ShutdownEventArgs> Shutdown;

        ///<summary>Fires when the server confirms successful consumer cancelation.</summary>
        public event EventHandler<ConsumerEventArgs> Unregistered;

        ///<summary>Fires when the server confirms successful consumer cancelation.</summary>
        public override void HandleBasicCancelOk(string consumerTag)
        {
            base.HandleBasicCancelOk(consumerTag);
            Unregistered?.Invoke(this, new ConsumerEventArgs(new[] { consumerTag }));
        }

        ///<summary>Fires when the server confirms successful consumer cancelation.</summary>
        public override void HandleBasicConsumeOk(string consumerTag)
        {
            base.HandleBasicConsumeOk(consumerTag);
            Registered?.Invoke(this, new ConsumerEventArgs(new[] { consumerTag }));
        }

        ///<summary>
        /// Invoked when a delivery arrives for the consumer.
        /// </summary>
        /// <remarks>
        /// Handlers must copy or fully use delivery body before returning.
        /// Accessing the body at a later point is unsafe as its memory can
        /// be already released.
        /// </remarks>
        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
            Received?.Invoke(
                this,
                new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body));
        }

        ///<summary>Fires the Shutdown event.</summary>
        public override void HandleModelShutdown(object model, ShutdownEventArgs reason)
        {
            base.HandleModelShutdown(model, reason);
            Shutdown?.Invoke(this, reason);
        }
    }
}
