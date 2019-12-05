using System.Collections.Generic;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Interface for constructing messages binary-compatible with QPid's "MapMessage" wire encoding.
    /// </summary>
    public interface IMapMessageBuilder : IMessageBuilder
    {
        /// <summary>
        /// Retrieves the dictionary that will be written into the body of the message.
        /// </summary>
        IDictionary<string, object> Body { get; }
    }
}
