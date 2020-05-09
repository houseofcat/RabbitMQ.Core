using System;

namespace RabbitMQ.Client.Exceptions
{
#if !NETSTANDARD1_5
    [Serializable]
#endif
    public class ProtocolViolationException : RabbitMQClientException
    {
        public ProtocolViolationException(string message) : base(message)
        {
        }
        public ProtocolViolationException(string message, Exception inner) : base(message, inner)
        {
        }
        public ProtocolViolationException()
        {
        }
    }
}
