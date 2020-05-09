using RabbitMQ.Client.Framing;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary> Thrown when our peer sends a frame that contains
    /// illegal values for one or more fields. </summary>
    public class SyntaxErrorException : HardProtocolException
    {
        public SyntaxErrorException(string message) : base(message)
        {
        }

        public override ushort ReplyCode
        {
            get { return Constants.SyntaxError; }
        }
    }
}
