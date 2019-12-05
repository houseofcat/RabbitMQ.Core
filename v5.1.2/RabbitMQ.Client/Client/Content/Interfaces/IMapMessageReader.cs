using System.Collections.Generic;

namespace RabbitMQ.Client.Content
{
    ///<summary>
    /// Analyzes messages binary-compatible with QPid's "MapMessage" wire encoding.
    /// </summary>
    public interface IMapMessageReader : IMessageReader
    {
        ///<summary>
        /// Parses the message body into an <see cref="IDictionary{TKey,TValue}"/> instance.
        /// </summary>
        IDictionary<string, object> Body { get; }
    }
}
