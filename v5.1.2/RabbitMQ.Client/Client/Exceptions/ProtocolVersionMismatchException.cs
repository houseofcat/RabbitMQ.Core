using System;

namespace RabbitMQ.Client.Exceptions
{
    ///<summary>Thrown to indicate that the peer does not support the
    ///wire protocol version we requested immediately after opening
    ///the TCP socket.</summary>
    public class ProtocolVersionMismatchException : ProtocolViolationException
    {
        ///<summary>Fills the new instance's properties with the values passed in.</summary>
        public ProtocolVersionMismatchException(int clientMajor,
            int clientMinor,
            int serverMajor,
            int serverMinor)
            : base($"AMQP server protocol negotiation failure: server version {positiveOrUnknown(serverMajor)}-{positiveOrUnknown(serverMinor)}, client version {positiveOrUnknown(clientMajor)}-{positiveOrUnknown(clientMinor)}")
        {
            ClientMajor = clientMajor;
            ClientMinor = clientMinor;
            ServerMajor = serverMajor;
            ServerMinor = serverMinor;
        }

        public ProtocolVersionMismatchException(string message) : base(message)
        {
        }

        public ProtocolVersionMismatchException(string message, Exception inner) : base(message, inner)
        {
        }

        public ProtocolVersionMismatchException()
        {
        }

        ///<summary>The client's AMQP specification major version.</summary>
        public int ClientMajor { get; }

        ///<summary>The client's AMQP specification minor version.</summary>
        public int ClientMinor { get; }

        ///<summary>The peer's AMQP specification major version.</summary>
        public int ServerMajor { get; }

        ///<summary>The peer's AMQP specification minor version.</summary>
        public int ServerMinor { get; }

        private static String positiveOrUnknown(int version)
        {
            return version >= 0 ? version.ToString() : "unknown";
        }
    }
}
