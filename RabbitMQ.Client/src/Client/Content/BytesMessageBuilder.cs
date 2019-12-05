namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Constructs AMQP Basic-class messages binary-compatible with QPid's "BytesMessage" wire encoding.
    /// </summary>
    public class BytesMessageBuilder : BasicMessageBuilder, IBytesMessageBuilder
    {
        /// <summary>
        /// MIME type associated with QPid BytesMessages.
        /// </summary>
        public const string MimeType = "application/octet-stream";

        /// <summary>
        /// Construct an instance for writing. See <see cref="BasicMessageBuilder"/>.
        /// </summary>
        public BytesMessageBuilder(IModel model)
            : base(model)
        {
        }

        /// <summary>
        /// Construct an instance for writing. See <see cref="BasicMessageBuilder"/>.
        /// </summary>
        public BytesMessageBuilder(IModel model, int initialAccumulatorSize)
            : base(model, initialAccumulatorSize)
        {
        }

        /// <summary>
        /// Write a section of a byte array into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder Write(byte[] source, int offset, int count)
        {
            BytesWireFormatting.Write(Writer, source, offset, count);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="byte"/> value into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteByte(byte value)
        {
            BytesWireFormatting.WriteByte(Writer, value);
            return this;
        }

        /// <summary>
        /// Write a <see cref="byte"/> array into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteBytes(byte[] source)
        {
            BytesWireFormatting.WriteBytes(Writer, source);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="char"/> value into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteChar(char value)
        {
            BytesWireFormatting.WriteChar(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="double"/> value into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteDouble(double value)
        {
            BytesWireFormatting.WriteDouble(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="short"/> value into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteInt16(short value)
        {
            BytesWireFormatting.WriteInt16(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes an <see cref="int"/> value into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteInt32(int value)
        {
            BytesWireFormatting.WriteInt32(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="long"/> value into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteInt64(long value)
        {
            BytesWireFormatting.WriteInt64(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="float"/> value into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteSingle(float value)
        {
            BytesWireFormatting.WriteSingle(Writer, value);
            return this;
        }

        /// <summary>
        /// Writes a <see cref="string"/> value into the message body being assembled.
        /// </summary>
        public IBytesMessageBuilder WriteString(string value)
        {
            BytesWireFormatting.WriteString(Writer, value);
            return this;
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
