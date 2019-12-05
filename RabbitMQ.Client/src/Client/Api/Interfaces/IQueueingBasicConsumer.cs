using RabbitMQ.Client.Events;
using RabbitMQ.Util;

namespace RabbitMQ.Client
{
    /// <summary>
    /// A <see cref="IBasicConsumer"/> implementation that
    /// uses a <see cref="SharedQueue"/> to buffer incoming deliveries.
    /// </summary>
    ///<remarks>
    ///<para>
    /// This interface is provided to make creation of test doubles
    /// for <see cref="QueueingBasicConsumer" /> easier.
    ///</para>
    ///</remarks>
    public interface IQueueingBasicConsumer
    {
        void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered,
            string exchange, string routingKey, IBasicProperties properties, byte[] body);

        void OnCancel();
        SharedQueue<BasicDeliverEventArgs> Queue { get; }
    }
}
