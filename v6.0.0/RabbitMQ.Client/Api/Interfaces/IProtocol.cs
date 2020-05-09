namespace RabbitMQ.Client
{
    /// <summary>
    /// Object describing various overarching parameters
    /// associated with a particular AMQP protocol variant.
    /// </summary>
    public interface IProtocol
    {
        /// <summary>
        /// Retrieve the protocol's API name, used for printing,
        /// configuration properties, IDE integration, Protocols.cs etc.
        /// </summary>
        string ApiName { get; }

        /// <summary>
        /// Retrieve the protocol's default TCP port.
        /// </summary>
        int DefaultPort { get; }

        /// <summary>
        /// Retrieve the protocol's major version number.
        /// </summary>
        int MajorVersion { get; }

        /// <summary>
        /// Retrieve the protocol's minor version number.
        /// </summary>
        int MinorVersion { get; }

        /// <summary>
        /// Retrieve the protocol's revision (if specified).
        /// </summary>
        int Revision { get; }
    }
}
