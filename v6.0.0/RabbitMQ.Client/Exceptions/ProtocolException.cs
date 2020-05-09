namespace RabbitMQ.Client.Exceptions
{
    /// <summary> Instances of subclasses of subclasses
    /// HardProtocolException and SoftProtocolException are thrown in
    /// situations when we detect a problem with the connection-,
    /// channel- or wire-level parts of the AMQP protocol. </summary>
    public abstract class ProtocolException : RabbitMQClientException
    {
        protected ProtocolException(string message) : base(message)
        {
        }

        ///<summary>Retrieve the reply code to use in a
        ///connection/channel close method.</summary>
        public abstract ushort ReplyCode { get; }

        ///<summary>Retrieve the shutdown details to use in a
        ///connection/channel close method. Defaults to using
        ///ShutdownInitiator.Library, and this.ReplyCode and
        ///this.Message as the reply code and text,
        ///respectively.</summary>
        public virtual ShutdownEventArgs ShutdownReason
        {
            get { return new ShutdownEventArgs(ShutdownInitiator.Library, ReplyCode, Message, this); }
        }
    }
}
