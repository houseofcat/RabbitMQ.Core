using System.Collections.Generic;
using System.IO;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Framework for constructing various types of AMQP. Basic-class application messages.
    /// </summary>
    public class BasicMessageBuilder : IMessageBuilder
    {
        /// <summary>
        /// By default, new instances of BasicMessageBuilder and its subclasses will have this much initial buffer space.
        /// </summary>
        public const int DefaultAccumulatorSize = 1024;

        protected MemoryStream m_accumulator;
        protected NetworkBinaryWriter m_writer;

        /// <summary>
        /// Construct an instance ready for writing.
        /// </summary>
        /// <remarks>
        /// The <see cref="DefaultAccumulatorSize"/> is used for the initial accumulator buffer size.
        /// </remarks>
        public BasicMessageBuilder(IModel model) : this(model, DefaultAccumulatorSize)
        {
        }

        /// <summary>
        /// Construct an instance ready for writing.
        /// </summary>
        public BasicMessageBuilder(IModel model, int initialAccumulatorSize)
        {
            Properties = model.CreateBasicProperties();
            m_accumulator = new MemoryStream(initialAccumulatorSize);

            string contentType = GetDefaultContentType();
            if (contentType != null)
            {
                Properties.ContentType = contentType;
            }
        }

        /// <summary>
        /// Retrieve the <see cref="IBasicProperties"/> associated with this instance.
        /// </summary>
        public IBasicProperties Properties { get; protected set; }

        /// <summary>
        /// Retrieve this instance's <see cref="NetworkBinaryWriter"/> writing to BodyStream.
        /// </summary>
        /// <remarks>
        /// If no <see cref="NetworkBinaryWriter"/> instance exists, one is created,
        /// pointing at the beginning of the accumulator. If one
        /// already exists, the existing instance is returned. The
        /// instance is not reset.
        /// </remarks>
        public NetworkBinaryWriter Writer
        {
            get
            {
                if (m_writer == null)
                {
                    m_writer = new NetworkBinaryWriter(m_accumulator);
                }
                return m_writer;
            }
        }

        /// <summary>
        /// Retrieve the <see cref="Stream"/> being used to construct the message body.
        /// </summary>
        public Stream BodyStream
        {
            get { return m_accumulator; }
        }

        /// <summary>
        /// Retrieves the dictionary that will be used to construct the message header table.
        /// It is of type <see cref="IDictionary{TKey,TValue}" />.
        /// </summary>
        public IDictionary<string, object> Headers
        {
            get
            {
                if (Properties.Headers == null)
                {
                    Properties.Headers = new Dictionary<string, object>();
                }
                return Properties.Headers;
            }
        }

        /// <summary>
        /// Finish and retrieve the content body for transmission.
        /// </summary>
        public virtual byte[] GetContentBody()
        {
            return m_accumulator.ToArray();
        }

        /// <summary>
        /// Returns the default MIME content type for messages this instance constructs,
        /// or null if none is available or relevant.
        /// </summary>
        public virtual IContentHeader GetContentHeader()
        {
            return Properties;
        }

        /// <summary>
        /// Returns the default MIME content type for messages this instance constructs,
        /// or null if none is available or relevant.
        /// </summary>
        public virtual string GetDefaultContentType()
        {
            return null;
        }

        /// <summary>
        /// Write a single byte into the message body, without encoding or interpretation.
        /// </summary>
        public IMessageBuilder RawWrite(byte value)
        {
            BodyStream.WriteByte(value);
            return this;
        }

        /// <summary>
        /// Write a byte array into the message body, without encoding or interpretation.
        /// </summary>
        public IMessageBuilder RawWrite(byte[] bytes)
        {
            return RawWrite(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Write a section of a byte array into the message body, without encoding or interpretation.
        /// </summary>
        public IMessageBuilder RawWrite(byte[] bytes, int offset, int length)
        {
            BodyStream.Write(bytes, offset, length);
            return this;
        }
    }
}
