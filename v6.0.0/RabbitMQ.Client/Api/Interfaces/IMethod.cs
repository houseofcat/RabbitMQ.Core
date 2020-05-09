namespace RabbitMQ.Client
{
    /// <summary>
    /// A decoded AMQP method frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// AMQP methods can be RPC requests, RPC responses, exceptions
    /// (ChannelClose, ConnectionClose), or one-way asynchronous
    /// messages. Currently this information is not recorded in their
    /// type or interface: it is implicit in the way the method is
    /// used, and the way it is defined in the AMQP specification. A
    /// future revision of the RabbitMQ .NET client library may extend
    /// the IMethod interface to represent this information
    /// explicitly.
    /// </para>
    /// </remarks>
    public interface IMethod
    {
        /// <summary>
        /// Retrieves the class ID number of this method, as defined in the AMQP specification XML.
        /// </summary>
        ushort ProtocolClassId { get; }

        /// <summary>
        /// Retrieves the method ID number of this method, as defined in the AMQP specification XML.
        /// </summary>
        ushort ProtocolMethodId { get; }

        /// <summary>
        /// Retrieves the name of this method - for debugging use.
        /// </summary>
        string ProtocolMethodName { get; }
    }
}
