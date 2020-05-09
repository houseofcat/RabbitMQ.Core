namespace RabbitMQ.Client
{
    ///<summary>
    /// Describes the source of a shutdown event.
    ///</summary>
    public enum ShutdownInitiator
    {
        ///<summary>
        /// The shutdown event originated from the application using the RabbitMQ .NET client library.
        ///</summary>
        Application,

        ///<summary>
        /// The shutdown event originated from the RabbitMQ .NET client library itself.
        ///</summary>
        ///<remarks>
        /// Shutdowns with this ShutdownInitiator code may appear if,
        /// for example, an internal error is detected by the client,
        /// or if the server sends a syntactically invalid
        /// frame. Another potential use is on IConnection AutoClose.
        ///</remarks>
        Library,

        ///<summary>
        /// The shutdown event originated from the remote AMQP peer.
        ///</summary>
        ///<remarks>
        /// A valid received connection.close or channel.close event
        /// will manifest as a shutdown with this ShutdownInitiator.
        ///</remarks>
        Peer
    };
}
