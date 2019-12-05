using System.Collections.Generic;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Constructs AMQP Basic-class messages binary-compatible with QPid's "MapMessage" wire encoding.
    /// </summary>
    public class MapMessageBuilder : BasicMessageBuilder, IMapMessageBuilder
    {
        /// <summary>
        /// MIME type associated with QPid MapMessages.
        /// </summary>
        public const string MimeType = "jms/map-message";

        /// <summary>
        /// Construct an instance for writing. See <see cref="BasicMessageBuilder"/>.
        /// </summary>
        public MapMessageBuilder(IModel model)
            : base(model)
        {
            Body = new Dictionary<string, object>();
        }

        /// <summary>
        /// Construct an instance for writing. See <see cref="BasicMessageBuilder"/>.
        /// </summary>
        public MapMessageBuilder(IModel model, int initialAccumulatorSize)
            : base(model, initialAccumulatorSize)
        {
            Body = new Dictionary<string, object>();
        }

        /// <summary>
        /// Retrieves the dictionary that will be written into the body of the message.
        /// </summary>
        public IDictionary<string, object> Body { get; protected set; }

        /// <summary>
        /// Finish and retrieve the content body for transmission.
        /// </summary>
        /// <remarks>
        /// Calling this message clears Body to null. Subsequent calls will fault.
        /// </remarks>
        public override byte[] GetContentBody()
        {
            MapWireFormatting.WriteMap(Writer, Body);
            var res = base.GetContentBody();
            Body = null;
            return res;
        }

        /// <summary>
        /// Returns the default MIME content type for messages this instance constructs,
        /// or null if none is available or relevant.
        /// </summary>
        public override string GetDefaultContentType()
        {
            return MimeType;
        }
    }
}
