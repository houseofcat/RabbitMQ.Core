using RabbitMQ.Client.Exceptions;
using System;

namespace RabbitMQ.Client
{
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