using System;

namespace RabbitMQ.Client.Exceptions
{
    public class TopologyRecoveryException : RabbitMQClientException
    {
        public TopologyRecoveryException(string message, Exception cause) : base(message, cause)
        {
        }

        protected TopologyRecoveryException()
        {
        }

        protected TopologyRecoveryException(string message) : base(message)
        {
        }
    }
}
