using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary>
    /// Thrown when the connection receives a frame that it wasn't expecting.
    /// </summary>
    public class UnexpectedFrameException : HardProtocolException
    {
        internal UnexpectedFrameException(Frame frame)
            : base("A frame of this type was not expected at this time")
        {
            Frame = frame;
        }

        internal Frame Frame { get; }

        public override ushort ReplyCode
        {
            get { return Constants.CommandInvalid; }
        }
    }
}
