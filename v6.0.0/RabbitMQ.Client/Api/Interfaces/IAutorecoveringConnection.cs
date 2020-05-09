using RabbitMQ.Client.Events;
using System;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Interface to an auto-recovering AMQP connection.
    /// </summary>
    public interface IAutorecoveringConnection : IConnection
    {
        event EventHandler<EventArgs> RecoverySucceeded;
        event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
    }
}
