using RabbitMQ.Client.Framing;
using System;

namespace RabbitMQ.Client.Impl
{
    /// <summary> Thrown when our peer sends a frame that contains
    /// illegal values for one or more fields. </summary>
    public class SyntaxError : HardProtocolException
    {
        public SyntaxError(string message) : base(message)
        {
        }

        protected SyntaxError()
        {
        }

        protected SyntaxError(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override ushort ReplyCode
        {
            get { return Constants.SyntaxError; }
        }
    }
}
