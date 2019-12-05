namespace RabbitMQ.Client
{
    /// <summary>
    /// A decoded AMQP content header frame.
    /// </summary>
    public interface IContentHeader// : ICloneable
    {
        /// <summary>
        /// Retrieve the AMQP class ID of this content header.
        /// </summary>
        int ProtocolClassId { get; }

        /// <summary>
        /// Retrieve the AMQP class name of this content header.
        /// </summary>
        string ProtocolClassName { get; }
    }
}
