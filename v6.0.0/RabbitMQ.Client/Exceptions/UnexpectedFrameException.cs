using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary>
    /// Thrown when the connection receives a frame that it wasn't expecting.
    /// </summary>
    public class UnexpectedFrameException : HardProtocolException
    {
        public UnexpectedFrameException(Frame frame)
            : base("A frame of this type was not expected at this time")
        {
            Frame = frame;
        }

        protected UnexpectedFrameException(string message) : base(message)
        {
        }

        protected UnexpectedFrameException() : base()
        {
        }

        protected UnexpectedFrameException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public Frame Frame { get; }

        public override ushort ReplyCode
        {
            get { return Constants.CommandInvalid; }
        }
    }
}
