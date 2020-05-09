using System;

namespace RabbitMQ.Client.Exceptions
{
    [Serializable]
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
