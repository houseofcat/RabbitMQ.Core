using RabbitMQ.Client.Events;
using System;
using System.Collections;

namespace RabbitMQ.Client.MessagePatterns
{
    ///<summary>Manages a subscription to a queue.</summary>
    ///<remarks>
    ///<para>
    /// This interface is provided to make creation of test doubles
    /// for <see cref="Subscription" /> easier.
    ///</para>
    ///</remarks>
    [Obsolete("Messaging patterns are no longer recommended for use and will be removed.")]
    public interface ISubscription : IEnumerable, IEnumerator, IDisposable
    {
        void Ack();
        void Ack(BasicDeliverEventArgs evt);
        void Close();
        IBasicConsumer Consumer { get; }
        string ConsumerTag { get; }
        BasicDeliverEventArgs LatestEvent { get; }
        IModel Model { get; }
        void Nack(BasicDeliverEventArgs evt, bool multiple, bool requeue);
        void Nack(bool multiple, bool requeue);
        void Nack(bool requeue);
        BasicDeliverEventArgs Next();
        bool Next(int millisecondsTimeout, out BasicDeliverEventArgs result);
        bool AutoAck { get; }
        string QueueName { get; }
    }
}
