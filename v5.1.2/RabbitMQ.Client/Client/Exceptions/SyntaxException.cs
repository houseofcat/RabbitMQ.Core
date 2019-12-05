using RabbitMQ.Client.Framing;
using System;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary> Thrown when our peer sends a frame that contains
    /// illegal values for one or more fields. </summary>
    public class SyntaxException : HardProtocolException
    {
        public SyntaxException(string message) : base(message)
        {
        }

        protected SyntaxException()
        {
        }

        protected SyntaxException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override ushort ReplyCode
        {
            get { return Constants.SyntaxError; }
        }
    }
}
