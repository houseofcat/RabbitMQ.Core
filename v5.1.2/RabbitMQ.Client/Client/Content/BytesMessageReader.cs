namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Analyzes AMQP Basic-class messages binary-compatible with QPid's "BytesMessage" wire encoding.
    /// </summary>
    public class BytesMessageReader : BasicMessageReader, IBytesMessageReader
    {
        /// <summary>
        /// MIME type associated with QPid BytesMessages.
        /// </summary>
        public static readonly string MimeType = BytesMessageBuilder.MimeType;

        // ^ repeated here for convenience

        /// <summary>
        /// Construct an instance for reading. See <see cref="BasicMessageReader"/>.
        /// </summary>
        public BytesMessageReader(IBasicProperties properties, byte[] payload)
            : base(properties, payload)
        {
        }

        /// <summary>
        /// Reads a given number ("count") of bytes from the message body,
        /// placing them into "target", starting at "offset".
        /// </summary>
        public int Read(byte[] target, int offset, int count)
        {
            return BytesWireFormatting.Read(Reader, target, offset, count);
        }

        /// <summary>
        /// Reads a <see cref="byte"/> from the message body.
        /// </summary>
        public byte ReadByte()
        {
            return BytesWireFormatting.ReadByte(Reader);
        }

        /// <summary>
        /// Reads a given number of bytes from the message body.
        /// </summary>
        public byte[] ReadBytes(int count)
        {
            return BytesWireFormatting.ReadBytes(Reader, count);
        }

        /// <summary>
        /// Reads a <see cref="char"/> from the message body.
        /// </summary>
        public char ReadChar()
        {
            return BytesWireFormatting.ReadChar(Reader);
        }

        /// <summary>
        /// Reads a <see cref="double"/> from the message body.
        /// </summary>
        public double ReadDouble()
        {
            return BytesWireFormatting.ReadDouble(Reader);
        }

        /// <summary>
        /// Reads a <see cref="short"/> from the message body.
        /// </summary>
        public short ReadInt16()
        {
            return BytesWireFormatting.ReadInt16(Reader);
        }

        /// <summary>
        /// Reads an <see cref="int"/> from the message body.
        /// </summary>
        public int ReadInt32()
        {
            return BytesWireFormatting.ReadInt32(Reader);
        }

        /// <summary>
        /// Reads a <see cref="long"/> from the message body.
        /// </summary>
        public long ReadInt64()
        {
            return BytesWireFormatting.ReadInt64(Reader);
        }

        /// <summary>
        /// Reads a <see cref="float"/> from the message body.
        /// </summary>
        public float ReadSingle()
        {
            return BytesWireFormatting.ReadSingle(Reader);
        }

        /// <summary>
        /// Reads a <see cref="string"/> from the message body.
        /// </summary>
        public string ReadString()
        {
            return BytesWireFormatting.ReadString(Reader);
        }
    }
}
