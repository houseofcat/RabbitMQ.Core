using System.Collections.Generic;
using System.IO;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Analyzes AMQP Basic-class messages binary-compatible with QPid's "StreamMessage" wire encoding.
    /// </summary>
    public class StreamMessageReader : BasicMessageReader, IStreamMessageReader
    {
        /// <summary>
        /// MIME type associated with QPid StreamMessages.
        /// </summary>
        public static readonly string MimeType = StreamMessageBuilder.MimeType;


        /// <summary>
        /// Construct an instance for reading. See <see cref="BasicMessageReader"/>.
        /// </summary>
        public StreamMessageReader(IBasicProperties properties, byte[] payload)
            : base(properties, payload)
        {
        }

        /// <summary>
        /// Reads a <see cref="bool"/> from the message body.
        /// </summary>
        public bool ReadBool()
        {
            return StreamWireFormatting.ReadBool(Reader);
        }

        /// <summary>
        /// Reads a <see cref="byte"/> from the message body.
        /// </summary>
        public byte ReadByte()
        {
            return StreamWireFormatting.ReadByte(Reader);
        }

        /// <summary>
        /// Reads a <see cref="byte"/> array from the message body.
        /// The body contains information about the size of the array to read.
        /// </summary>
        public byte[] ReadBytes()
        {
            return StreamWireFormatting.ReadBytes(Reader);
        }

        /// <summary>
        /// Reads a <see cref="char"/> from the message body.
        /// </summary>
        public char ReadChar()
        {
            return StreamWireFormatting.ReadChar(Reader);
        }

        /// <summary>
        /// Reads a <see cref="double"/> from the message body.
        /// </summary>
        public double ReadDouble()
        {
            return StreamWireFormatting.ReadDouble(Reader);
        }

        /// <summary>
        /// Reads a <see cref="short"/> from the message body.
        /// </summary>
        public short ReadInt16()
        {
            return StreamWireFormatting.ReadInt16(Reader);
        }

        /// <summary>
        /// Reads an <see cref="int"/> from the message body.
        /// </summary>
        public int ReadInt32()
        {
            return StreamWireFormatting.ReadInt32(Reader);
        }

        /// <summary>
        /// Reads a <see cref="long"/> from the message body.
        /// </summary>
        public long ReadInt64()
        {
            return StreamWireFormatting.ReadInt64(Reader);
        }

        /// <summary>
        /// Reads an <see cref="object"/> from the message body.
        /// </summary>
        public object ReadObject()
        {
            return StreamWireFormatting.ReadObject(Reader);
        }

        /// <summary>
        /// Reads <see cref="object"/> array from the message body until the end-of-stream is reached.
        /// </summary>
        public object[] ReadObjects()
        {
            var result = new List<object>();
            while (true)
            {
                try
                {
                    object val = ReadObject();
                    result.Add(val);
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Reads a <see cref="float"/> from the message body.
        /// </summary>
        public float ReadSingle()
        {
            return StreamWireFormatting.ReadSingle(Reader);
        }

        /// <summary>
        /// Reads a <see cref="string"/> from the message body.
        /// </summary>
        public string ReadString()
        {
            return StreamWireFormatting.ReadString(Reader);
        }
    }
}
