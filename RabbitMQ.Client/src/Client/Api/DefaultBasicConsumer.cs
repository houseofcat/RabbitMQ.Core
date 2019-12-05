using RabbitMQ.Client.Events;
using System;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Useful default/base implementation of <see cref="IBasicConsumer"/>.
    /// Subclass and override <see cref="HandleBasicDeliver"/> in application code.
    /// </summary>
    /// <remarks>
    /// Note that the "Handle*" methods run in the connection's thread!
    /// Consider using <see cref="EventingBasicConsumer"/>,  which exposes
    /// events that can be subscribed to consumer messages.
    /// </remarks>
    public class DefaultBasicConsumer : IBasicConsumer
    {
        public readonly object m_eventLock = new object();
        public EventHandler<ConsumerEventArgs> m_consumerCancelled;

        /// <summary>
        /// Creates a new instance of an <see cref="DefaultBasicConsumer"/>.
        /// </summary>
        public DefaultBasicConsumer()
        {
            ShutdownReason = null;
            Model = null;
            IsRunning = false;
            ConsumerTag = null;
        }

        /// <summary>
        /// Constructor which sets the Model property to the given value.
        /// </summary>
        /// <param name="model">Common AMQP model.</param>
        public DefaultBasicConsumer(IModel model)
        {
            ShutdownReason = null;
            IsRunning = false;
            ConsumerTag = null;
            Model = model;
        }

        /// <summary>
        /// Retrieve the consumer tag this consumer is registered as; to be used when discussing this consumer
        /// with the server, for instance with <see cref="IModel.BasicCancel"/>.
        /// </summary>
        public string ConsumerTag { get; set; }

        /// <summary>
        /// Returns true while the consumer is registered and expecting deliveries from the broker.
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// If our <see cref="IModel"/> shuts down, this property will contain a description of the reason for the
        /// shutdown. Otherwise it will contain null. See <see cref="ShutdownEventArgs"/>.
        /// </summary>
        public ShutdownEventArgs ShutdownReason { get; protected set; }

        /// <summary>
        /// Signalled when the consumer gets cancelled.
        /// </summary>
        public event EventHandler<ConsumerEventArgs> ConsumerCancelled
        {
            add
            {
                lock (m_eventLock)
                {
                    m_consumerCancelled += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    m_consumerCancelled -= value;
                }
            }
        }

        /// <summary>
        /// Retrieve the <see cref="IModel"/> this consumer is associated with,
        ///  for use in acknowledging received messages, for instance.
        /// </summary>
        public IModel Model { get; set; }

        /// <summary>
        ///  Called when the consumer is cancelled for reasons other than by a basicCancel:
        ///  e.g. the queue has been deleted (either by this channel or  by any other channel).
        ///  See <see cref="HandleBasicCancelOk"/> for notification of consumer cancellation due to basicCancel
        /// </summary>
        /// <param name="consumerTag">Consumer tag this consumer is registered.</param>
        public virtual void HandleBasicCancel(string consumerTag)
        {
            OnCancel();
        }

        /// <summary>
        /// Called upon successful deregistration of the consumer from the broker.
        /// </summary>
        /// <param name="consumerTag">Consumer tag this consumer is registered.</param>
        public virtual void HandleBasicCancelOk(string consumerTag)
        {
            OnCancel();
        }

        /// <summary>
        /// Called upon successful registration of the consumer with the broker.
        /// </summary>
        /// <param name="consumerTag">Consumer tag this consumer is registered.</param>
        public virtual void HandleBasicConsumeOk(string consumerTag)
        {
            ConsumerTag = consumerTag;
            IsRunning = true;
        }

        /// <summary>
        /// Called each time a message arrives for this consumer.
        /// </summary>
        /// <remarks>
        /// Does nothing with the passed in information.
        /// Note that in particular, some delivered messages may require acknowledgement via <see cref="IModel.BasicAck"/>.
        /// The implementation of this method in this class does NOT acknowledge such messages.
        /// </remarks>
        public virtual void HandleBasicDeliver(string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            byte[] body)
        {
            // Nothing to do here.
        }

        /// <summary>
        ///  Called when the model shuts down.
        ///  </summary>
        ///  <param name="model"> Common AMQP model.</param>
        /// <param name="reason"> Information about the reason why a particular model, session, or connection was destroyed.</param>
        public virtual void HandleModelShutdown(object model, ShutdownEventArgs reason)
        {
            ShutdownReason = reason;
            OnCancel();
        }

        /// <summary>
        /// Default implementation - overridable in subclasses.</summary>
        /// <remarks>
        /// This default implementation simply sets the <see cref="IsRunning"/> 
        /// property to false, and takes no further action.
        /// </remarks>
        public virtual void OnCancel()
        {
            IsRunning = false;
            EventHandler<ConsumerEventArgs> handler;
            lock (m_eventLock)
            {
                handler = m_consumerCancelled;
            }
            if (handler != null)
            {
                foreach (EventHandler<ConsumerEventArgs> h in handler.GetInvocationList())
                {
                    h(this, new ConsumerEventArgs(ConsumerTag));
                }
            }
        }
    }
}
