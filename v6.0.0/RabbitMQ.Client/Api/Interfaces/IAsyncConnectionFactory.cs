namespace RabbitMQ.Client
{
    /// <summary>
    /// Defines a connection factory capable of using an asynchronous consumer dispatcher which is compatible with <see cref="IAsyncBasicConsumer"/>.
    /// </summary>
    /// <seealso cref="IConnectionFactory" />
    public interface IAsyncConnectionFactory : IConnectionFactory
    {
        /// <summary>
        /// Gets or sets a value indicating whether an asynchronous consumer dispatcher which is compatible with <see cref="IAsyncBasicConsumer"/> is used.
        /// </summary>
        /// <value><see langword="true" /> if an asynchronous consumer dispatcher which is compatible with <see cref="IAsyncBasicConsumer"/> is used; otherwise, <see langword="false" />.</value>
        bool DispatchConsumersAsync { get; set; }
    }
}