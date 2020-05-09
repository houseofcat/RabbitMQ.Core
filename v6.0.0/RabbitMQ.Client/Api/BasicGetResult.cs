using System;

namespace RabbitMQ.Client
{
    /// <summary>Represents Basic.GetOk responses from the server.</summary>
    /// <remarks>
    /// Basic.Get either returns an instance of this class, or null if a Basic.GetEmpty was received.
    /// </remarks>
    public class BasicGetResult
    {
        /// <summary>
        /// Sets the new instance's properties from the arguments passed in.
        /// </summary>
        /// <param name="deliveryTag">Delivery tag for the message.</param>
        /// <param name="redelivered">Redelivered flag for the message</param>
        /// <param name="exchange">The exchange this message was published to.</param>
        /// <param name="routingKey">Routing key with which the message was published.</param>
        /// <param name="messageCount">The number of messages pending on the queue, excluding the message being delivered.</param>
        /// <param name="basicProperties">The Basic-class content header properties for the message.</param>
        /// <param name="body"></param>
        public BasicGetResult(ulong deliveryTag, bool redelivered, string exchange,
            string routingKey, uint messageCount, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            DeliveryTag = deliveryTag;
            Redelivered = redelivered;
            Exchange = exchange;
            RoutingKey = routingKey;
            MessageCount = messageCount;
            BasicProperties = basicProperties;
            Body = body;
        }

        /// <summary>
        /// Retrieves the Basic-class content header properties for this message.
        /// </summary>
        public IBasicProperties BasicProperties { get; private set; }

        /// <summary>
        /// Retrieves the body of this message.
        /// </summary>
        public ReadOnlyMemory<byte> Body { get; private set; }

        /// <summary>
        /// Retrieve the delivery tag for this message. See also <see cref="IModel.BasicAck"/>.
        /// </summary>
        public ulong DeliveryTag { get; private set; }

        /// <summary>
        /// Retrieve the exchange this message was published to.
        /// </summary>
        public string Exchange { get; private set; }

        /// <summary>
        /// Retrieve the number of messages pending on the queue, excluding the message being delivered.
        /// </summary>
        /// <remarks>
        /// Note that this figure is indicative, not reliable, and can
        /// change arbitrarily as messages are added to the queue and removed by other clients.
        /// </remarks>
        public uint MessageCount { get; private set; }

        /// <summary>
        /// Retrieve the redelivered flag for this message.
        /// </summary>
        public bool Redelivered { get; private set; }

        /// <summary>
        /// Retrieve the routing key with which this message was published.
        /// </summary>
        public string RoutingKey { get; private set; }
    }
}
