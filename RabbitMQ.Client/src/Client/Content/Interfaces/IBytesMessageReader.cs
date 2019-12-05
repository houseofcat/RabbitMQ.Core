namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Analyzes messages binary-compatible with QPid's "BytesMessage" wire encoding.
    /// </summary>
    public interface IBytesMessageReader : IMessageReader
    {
        /// <summary>
        /// Reads a given number ("count") of bytes from the message body,
        /// placing them into "target", starting at "offset".
        /// </summary>
        int Read(byte[] target, int offset, int count);

        /// <summary>
        /// Reads a <see cref="byte"/> from the message body.
        /// </summary>
        byte ReadByte();

        /// <summary>
        /// Reads a given number of bytes from the message body.
        /// </summary>
        byte[] ReadBytes(int count);

        /// <summary>
        /// Reads a <see cref="char"/> from the message body.
        /// </summary>
        char ReadChar();

        /// <summary>
        /// Reads a <see cref="double"/> from the message body.
        /// </summary>
        double ReadDouble();

        /// <summary>
        /// Reads a <see cref="short"/> from the message body.
        /// </summary>
        short ReadInt16();

        /// <summary>
        /// Reads an <see cref="int"/> from the message body.
        /// </summary>
        int ReadInt32();

        /// <summary>
        /// Reads a <see cref="long"/> from the message body.
        /// </summary>
        long ReadInt64();

        /// <summary>
        /// Reads a <see cref="float"/> from the message body.
        /// </summary>
        float ReadSingle();

        /// <summary>
        /// Reads a <see cref="string"/> from the message body.
        /// </summary>
        string ReadString();
    }
}
