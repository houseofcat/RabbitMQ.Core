using RabbitMQ.Client.Events;
using RabbitMQ.Util;
using System;

namespace RabbitMQ.Client
{
    /// <summary>
    /// A <see cref="IBasicConsumer"/> implementation that
    /// uses a <see cref="SharedQueue"/> to buffer incoming deliveries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Received messages are placed in the SharedQueue as instances
    /// of <see cref="BasicDeliverEventArgs"/>.
    /// </para>
    /// <para>
    /// Note that messages taken from the SharedQueue may need
    /// acknowledging with <see cref="IModel.BasicAck"/>.
    /// </para>
    /// <para>
    /// When the consumer is closed, through BasicCancel or through
    /// the shutdown of the underlying <see cref="IModel"/> or <see cref="IConnection"/>,
    ///  the  <see cref="SharedQueue{T}.Close"/> method is called, which causes any
    /// Enqueue() operations, and Dequeue() operations when the queue
    /// is empty, to throw EndOfStreamException (see the comment for <see cref="SharedQueue{T}.Close"/>).
    /// </para>
    /// <para>
    /// The following is a simple example of the usage of this class:
    /// </para>
    /// <example><code>
    /// IModel channel = ...;
    /// QueueingBasicConsumer consumer = new QueueingBasicConsumer(channel);
    /// channel.BasicConsume(queueName, null, consumer);
    ///
    /// // At this point, messages will be being asynchronously delivered,
    /// // and will be queueing up in consumer.Queue.
    ///
    /// while (true) {
    ///     try {
    ///         BasicDeliverEventArgs e = (BasicDeliverEventArgs) consumer.Queue.Dequeue();
    ///         // ... handle the delivery ...
    ///         channel.BasicAck(e.DeliveryTag, false);
    ///     } catch (EndOfStreamException ex) {
    ///         // The consumer was cancelled, the model closed, or the
    ///         // connection went away.
    ///         break;
    ///     }
    /// }
    /// </code></example>
    /// </remarks>
    [Obsolete("Deprecated. Use EventingBasicConsumer or a different consumer interface implementation instead")]
    public class QueueingBasicConsumer : DefaultBasicConsumer, IQueueingBasicConsumer
    {
        /// <summary>
        /// Creates a fresh <see cref="QueueingBasicConsumer"/>,
        ///  initialising the <see cref="DefaultBasicConsumer.Model"/> property to null
        ///  and the <see cref="Queue"/> property to a fresh <see cref="SharedQueue"/>.
        /// </summary>
        public QueueingBasicConsumer() : this(null)
        {
        }

        /// <summary>
        /// Creates a fresh <see cref="QueueingBasicConsumer"/>, with <see cref="DefaultBasicConsumer.Model"/>
        ///  set to the argument, and <see cref="Queue"/> set to a fresh <see cref="SharedQueue"/>.
        /// </summary>
        public QueueingBasicConsumer(IModel model) : this(model, new SharedQueue<BasicDeliverEventArgs>())
        {
        }

        /// <summary>
        /// Creates a fresh <see cref="QueueingBasicConsumer"/>,
        ///  initialising the <see cref="DefaultBasicConsumer.Model"/>
        ///  and <see cref="Queue"/> properties to the given values.
        /// </summary>
        public QueueingBasicConsumer(IModel model, SharedQueue<BasicDeliverEventArgs> queue) : base(model)
        {
            Queue = queue;
        }

        /// <summary>
        /// Retrieves the <see cref="SharedQueue"/> that messages arrive on.
        /// </summary>
        public SharedQueue<BasicDeliverEventArgs> Queue { get; protected set; }

        /// <summary>
        /// Overrides <see cref="DefaultBasicConsumer"/>'s  <see cref="HandleBasicDeliver"/> implementation,
        ///  building a <see cref="BasicDeliverEventArgs"/> instance and placing it in the Queue.
        /// </summary>
        public override void HandleBasicDeliver(string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            byte[] body)
        {
            var eventArgs = new BasicDeliverEventArgs
            {
                ConsumerTag = consumerTag,
                DeliveryTag = deliveryTag,
                Redelivered = redelivered,
                Exchange = exchange,
                RoutingKey = routingKey,
                BasicProperties = properties,
                Body = body
            };
            Queue.Enqueue(eventArgs);
        }

        /// <summary>
        /// Overrides <see cref="DefaultBasicConsumer"/>'s OnCancel implementation,
        ///  extending it to call the Close() method of the <see cref="SharedQueue"/>.
        /// </summary>
        public override void OnCancel()
        {
            base.OnCancel();
            Queue.Close();
        }
    }
}
