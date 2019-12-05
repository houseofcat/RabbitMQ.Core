namespace RabbitMQ.Client
{
    /// <summary>
    /// Common interface for network (TCP/IP) connection classes.
    /// </summary>
    public interface NetworkConnection
    {
        /// <summary>
        /// Local port.
        /// </summary>
        int LocalPort { get; }

        /// <summary>
        /// Remote port.
        /// </summary>
        int RemotePort { get; }
    }
}
