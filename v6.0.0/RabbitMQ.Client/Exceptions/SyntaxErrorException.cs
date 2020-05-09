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

        protected SyntaxErrorException() : base()
        {
        }

        protected SyntaxErrorException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public override ushort ReplyCode
        {
            get { return Constants.SyntaxError; }
        }
    }
}
