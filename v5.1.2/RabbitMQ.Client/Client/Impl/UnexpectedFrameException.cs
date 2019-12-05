using RabbitMQ.Client.Framing;

namespace RabbitMQ.Client.Impl
{
    /// <summary>
    /// Thrown when the connection receives a frame that it wasn't expecting.
    /// </summary>
    public class UnexpectedFrameException : HardProtocolException
    {
        public Frame m_frame;

        public UnexpectedFrameException(Frame frame)
            : base("A frame of this type was not expected at this time")
        {
            m_frame = frame;
        }

        protected UnexpectedFrameException(string message) : base(message)
        {
        }

        protected UnexpectedFrameException()
        {
        }

        protected UnexpectedFrameException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public Frame Frame
        {
            get { return m_frame; }
        }

        public override ushort ReplyCode
        {
            get { return Constants.CommandInvalid; }
        }
    }
}
