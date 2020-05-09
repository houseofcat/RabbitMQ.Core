using System;

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
#if !NETSTANDARD1_5
    [Serializable]
#endif
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

        ///<summary>The peer's AMQP specification major version.</summary>
        public int ServerMajor { get; private set; }

        ///<summary>The peer's AMQP specification minor version.</summary>
        public int ServerMinor { get; private set; }

        ///<summary>The peer's high transport byte.</summary>
        public int TransportHigh { get; private set; }

        ///<summary>The peer's low transport byte.</summary>
        public int TransportLow { get; private set; }
    }
}
