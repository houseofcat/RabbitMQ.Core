namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Interface for constructing messages binary-compatible with QPid's "BytesMessage" wire encoding.
    /// </summary>
    public interface IBytesMessageBuilder : IMessageBuilder
    {
        /// <summary>
        /// Write a section of a byte array into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder Write(byte[] source, int offset, int count);

        /// <summary>
        /// Writes a <see cref="byte"/> value into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteByte(byte value);

        /// <summary>
        /// Write a <see cref="byte"/> array into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteBytes(byte[] source);

        /// <summary>
        /// Writes a <see cref="char"/> value into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteChar(char value);

        /// <summary>
        /// Writes a <see cref="double"/> value into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteDouble(double value);

        /// <summary>
        /// Writes a <see cref="short"/> value into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteInt16(short value);

        /// <summary>
        /// Writes an <see cref="int"/> value into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteInt32(int value);

        /// <summary>
        /// Writes a <see cref="long"/> value into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteInt64(long value);

        /// <summary>
        /// Writes a <see cref="float"/> value into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteSingle(float value);

        /// <summary>
        /// Writes a <see cref="string"/> value into the message body being assembled.
        /// </summary>
        IBytesMessageBuilder WriteString(string value);
    }
}
