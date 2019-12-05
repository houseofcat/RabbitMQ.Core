using RabbitMQ.Util;
using System.Text;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Internal support class for use in reading and
    /// writing information binary-compatible with QPid's "BytesMessage" wire encoding.
    /// </summary>
    public static class BytesWireFormatting
    {
        public static int Read(NetworkBinaryReader reader, byte[] target, int offset, int count)
        {
            return reader.Read(target, offset, count);
        }

        public static byte ReadByte(NetworkBinaryReader reader)
        {
            return reader.ReadByte();
        }

        public static byte[] ReadBytes(NetworkBinaryReader reader, int count)
        {
            return reader.ReadBytes(count);
        }

        public static char ReadChar(NetworkBinaryReader reader)
        {
            return (char)reader.ReadUInt16();
        }

        public static double ReadDouble(NetworkBinaryReader reader)
        {
            return reader.ReadDouble();
        }

        public static short ReadInt16(NetworkBinaryReader reader)
        {
            return reader.ReadInt16();
        }

        public static int ReadInt32(NetworkBinaryReader reader)
        {
            return reader.ReadInt32();
        }

        public static long ReadInt64(NetworkBinaryReader reader)
        {
            return reader.ReadInt64();
        }

        public static float ReadSingle(NetworkBinaryReader reader)
        {
            return reader.ReadSingle();
        }

        public static string ReadString(NetworkBinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static void Write(NetworkBinaryWriter writer, byte[] source, int offset, int count)
        {
            writer.Write(source, offset, count);
        }

        public static void WriteByte(NetworkBinaryWriter writer, byte value)
        {
            writer.Write(value);
        }

        public static void WriteBytes(NetworkBinaryWriter writer, byte[] source)
        {
            Write(writer, source, 0, source.Length);
        }

        public static void WriteChar(NetworkBinaryWriter writer, char value)
        {
            writer.Write((ushort)value);
        }

        public static void WriteDouble(NetworkBinaryWriter writer, double value)
        {
            writer.Write(value);
        }

        public static void WriteInt16(NetworkBinaryWriter writer, short value)
        {
            writer.Write(value);
        }

        public static void WriteInt32(NetworkBinaryWriter writer, int value)
        {
            writer.Write(value);
        }

        public static void WriteInt64(NetworkBinaryWriter writer, long value)
        {
            writer.Write(value);
        }

        public static void WriteSingle(NetworkBinaryWriter writer, float value)
        {
            writer.Write(value);
        }

        public static void WriteString(NetworkBinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);
        }
    }
}
