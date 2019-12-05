namespace RabbitMQ.Client.Impl
{
    ///<summary>Subclass of ProtocolException representing problems
    ///requiring a channel.close.</summary>
    public abstract class SoftProtocolException : ProtocolException
    {
        protected SoftProtocolException(int channelNumber, string message)
            : base(message)
        {
            Channel = channelNumber;
        }

        protected SoftProtocolException(string message) : base(message)
        {
        }

        protected SoftProtocolException()
        {
        }

        protected SoftProtocolException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        public int Channel { get; }
    }
}
