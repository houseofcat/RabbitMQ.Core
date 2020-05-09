namespace RabbitMQ.Client.Exceptions
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

        public int Channel { get; }
    }
}
