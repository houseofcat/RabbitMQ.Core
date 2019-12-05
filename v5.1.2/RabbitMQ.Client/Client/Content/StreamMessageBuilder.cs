namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Constructs AMQP Basic-class messages binary-compatible with QPid's "StreamMessage" wire encoding.
    /// </summary>
    public class StreamMessageBuilder : BasicMessageBuilder, IStreamMessageBuilder
    {
        /// <summary>
        /// MIME type associated with QPid StreamMessages.
        /// </summary>
        public const string MimeType = "jms/stream-message";

        /// <summary>
        /// Construct an instance for writing. See <see cref="BasicMessageBuilder"/>.
        /// </summary>
        public StreamMessageBuilder(IModel model)
            : base(model)
        {
        }

        /// <summary>
        /// Construct an instance for writing. See <see cref="BasicMessageBuilder"/>.
        /// </summary>
        public StreamMessageBuilder(IModel model, int initialAccumulatorSize)
            : base(model, initialAccumulatorSize)
        {
        }

        /// <summary>
        /// Returns the default MIME content type for messages this instance constructs,
        /// or null if none is available or relevant.
        /// </summary>
        public override string GetDefaultContentType()
        {
            return MimeType;
        }

        /// <summary>
        /// Writes a <see cref="bool"/> value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteBool(bool value)
        {
            StreamWireFormatting.WriteBool(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="byte"/> value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteByte(byte value)
        {
            StreamWireFormatting.WriteByte(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a section of a byte array into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteBytes(byte[] source, int offset, int count)
        {
            StreamWireFormatting.WriteBytes(Writer, source, offset, count);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="byte"/> array into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteBytes(byte[] source)
        {
            StreamWireFormatting.WriteBytes(Writer, source);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="char"/> value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteChar(char value)
        {
            StreamWireFormatting.WriteChar(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="double"/> value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteDouble(double value)
        {
            StreamWireFormatting.WriteDouble(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="short"/> value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteInt16(short value)
        {
            StreamWireFormatting.WriteInt16(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes an <see cref="int"/> value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteInt32(int value)
        {
            StreamWireFormatting.WriteInt32(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="long"/> value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteInt64(long value)
        {
            StreamWireFormatting.WriteInt64(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes an <see cref="object"/> value into the message body being assembled.
        /// </summary>
        /// <remarks>
        /// The only permitted types are bool, int, short, byte, char,
        /// long, float, double, byte[] and string.
        /// </remarks>
        public IStreamMessageBuilder WriteObject(object value)
        {
            StreamWireFormatting.WriteObject(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes objects using WriteObject(), one after the other. No length indicator is written.
        /// See also <see cref="IStreamMessageReader.ReadObjects"/>.
        /// </summary>
        public IStreamMessageBuilder WriteObjects(params object[] values)
        {
            foreach (object val in values)
            {
                WriteObject(val);
            }
            return this;
        }

        /// <summary>
        /// Writes a <see cref="float"/> value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteSingle(float value)
        {
            StreamWireFormatting.WriteSingle(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="float"/>string value into the message body being assembled.
        /// </summary>
        public IStreamMessageBuilder WriteString(string value)
        {
            StreamWireFormatting.WriteString(Writer, value);
            return this;
        }
    }
}
