using System.Collections.Generic;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Analyzes AMQP Basic-class messages binary-compatible with QPid's "MapMessage" wire encoding.
    /// </summary>
    public class MapMessageReader : BasicMessageReader, IMapMessageReader
    {
        /// <summary>
        /// MIME type associated with QPid MapMessages.
        /// </summary>
        public const string MimeType = MapMessageBuilder.MimeType;

        protected IDictionary<string, object> m_table;

        /// <summary>
        /// Construct an instance for reading. See <see cref="BasicMessageReader"/>.
        /// </summary>
        public MapMessageReader(IBasicProperties properties, byte[] payload)
            : base(properties, payload)
        {
        }

        ///<summary>
        /// Parses the message body into an <see cref="IDictionary{TKey,TValue}"/> instance.
        /// </summary>
        /// <exception cref="ProtocolViolationException"/>.
        public IDictionary<string, object> Body
        {
            get
            {
                if (m_table == null)
                {
                    m_table = MapWireFormatting.ReadMap(Reader);
                }
                return m_table;
            }
        }
    }
}
