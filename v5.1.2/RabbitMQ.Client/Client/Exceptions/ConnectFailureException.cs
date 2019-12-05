using System;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary>Thrown when a connection to the broker fails</summary>
    public class ConnectFailureException : ProtocolViolationException
    {
        public ConnectFailureException(string msg, Exception inner)
            : base(msg, inner)
        {
        }

        public ConnectFailureException(string message) : base(message)
        {
        }

        public ConnectFailureException()
        {
        }
    }
}
