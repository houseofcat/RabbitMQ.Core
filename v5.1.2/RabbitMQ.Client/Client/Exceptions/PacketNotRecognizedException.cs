namespace RabbitMQ.Client.Exceptions
{
    ///<summary>Thrown to indicate that the peer didn't understand
    ///the packet received from the client. Peer sent default message
    ///describing protocol version it is using and transport parameters.
    ///</summary>
    ///<remarks>
    ///The peer's {'A','M','Q','P',txHi,txLo,major,minor} packet is
    ///decoded into instances of this class.
    ///</remarks>
    public class PacketNotRecognizedException : RabbitMQClientException
    {
        ///<summary>Fills the new instance's properties with the values passed in.</summary>
        public PacketNotRecognizedException(int transportHigh,
            int transportLow,
            int serverMajor,
            int serverMinor)
            : base($"AMQP server protocol version {serverMajor}-{serverMinor}, transport parameters {transportHigh}:{transportLow}")
        {
            TransportHigh = transportHigh;
            TransportLow = transportLow;
            ServerMajor = serverMajor;
            ServerMinor = serverMinor;
        }

        protected PacketNotRecognizedException()
        {
        }

        protected PacketNotRecognizedException(string message) : base(message)
        {
        }

        protected PacketNotRecognizedException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        ///<summary>The peer's AMQP specification major version.</summary>
        public int ServerMajor { get; }

        ///<summary>The peer's AMQP specification minor version.</summary>
        public int ServerMinor { get; }

        ///<summary>The peer's high transport byte.</summary>
        public int TransportHigh { get; }

        ///<summary>The peer's low transport byte.</summary>
        public int TransportLow { get; }
    }
}
