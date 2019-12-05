using RabbitMQ.Client.Framing;

namespace RabbitMQ.Client.Impl
{
    ///<summary>Thrown when frame parsing code detects an error in the
    ///wire-protocol encoding of a frame.</summary>
    ///<remarks>
    ///For example, potential MalformedFrameException conditions
    ///include frames too short, frames missing their end marker, and
    ///invalid protocol negotiation headers.
    ///</remarks>
    public class MalformedFrameException : HardProtocolException
    {
        public MalformedFrameException(string message) : base(message)
        {
        }

        protected MalformedFrameException()
        {
        }

        protected MalformedFrameException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public override ushort ReplyCode
        {
            get { return Constants.FrameError; }
        }
    }
}
