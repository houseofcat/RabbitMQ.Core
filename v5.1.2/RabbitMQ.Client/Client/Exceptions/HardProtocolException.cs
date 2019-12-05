namespace RabbitMQ.Client.Exceptions
{
    ///<summary>Subclass of ProtocolException representing problems
    ///requiring a connection.close.</summary>
    public abstract class HardProtocolException : ProtocolException
    {
        protected HardProtocolException(string message) : base(message)
        {
        }

        protected HardProtocolException()
        {
        }

        protected HardProtocolException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
