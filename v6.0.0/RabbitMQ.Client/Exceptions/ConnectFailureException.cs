using System;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary>Thrown when a connection to the broker fails</summary>
#if !NETSTANDARD1_5
    [Serializable]
#endif
    public class ConnectFailureException : ProtocolViolationException
    {
        public ConnectFailureException(string msg, Exception inner)
            : base(msg, inner)
        {
        }
    }
}
