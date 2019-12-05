namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Analyzes messages binary-compatible with QPid's "StreamMessage" wire encoding.
    /// </summary>
    public interface IStreamMessageReader : IMessageReader
    {
        /// <summary>
        /// Reads a <see cref="bool"/> from the message body.
        /// </summary>
        bool ReadBool();

        /// <summary>
        /// Reads a <see cref="byte"/> from the message body.
        /// </summary>
        byte ReadByte();

        /// <summary>
        /// Reads a <see cref="byte"/> array from the message body.
        /// The body contains information about the size of the array to read.
        /// </summary>
        byte[] ReadBytes();

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
        /// Reads an <see cref="object"/> from the message body.
        /// </summary>
        object ReadObject();

        /// <summary>
        /// Reads <see cref="object"/> array from the message body until the end-of-stream is reached.
        /// </summary>
        object[] ReadObjects();

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
