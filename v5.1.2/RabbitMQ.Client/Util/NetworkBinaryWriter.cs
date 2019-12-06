using System.IO;
using System.Text;

namespace RabbitMQ.Util
{
    /// <summary>
    /// Subclass of BinaryWriter that writes integers etc in correct network order.
    /// </summary>
    ///
    /// <remarks>
    /// <p>
    /// Kludge to compensate for .NET's broken little-endian-only BinaryWriter.
    /// </p><p>
    /// See also NetworkBinaryReader.
    /// </p>
    /// </remarks>
    public class NetworkBinaryWriter : BinaryWriter
    {
        /// <summary>
        /// Construct a NetworkBinaryWriter over the given input stream.
        /// </summary>
        public NetworkBinaryWriter(Stream output) : base(output)
        {
        }

        /// <summary>
        /// Construct a NetworkBinaryWriter over the given input
        /// stream, reading strings using the given encoding.
        /// </summary>
        public NetworkBinaryWriter(Stream output, Encoding encoding) : base(output, encoding)
        {
        }

        ///<summary>Helper method for constructing a temporary
        ///BinaryWriter streaming into a fresh MemoryStream
        ///provisioned with the given initialSize.</summary>
        public static BinaryWriter TemporaryBinaryWriter(int initialSize)
        {
            return new BinaryWriter(new MemoryStream(initialSize));
        }

        ///<summary>Helper method for extracting the byte[] contents
        ///of a BinaryWriter over a MemoryStream, such as constructed
        ///by TemporaryBinaryWriter.</summary>
        public static byte[] TemporaryContents(BinaryWriter w)
        {
            return ((MemoryStream)w.BaseStream).ToArray();
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public override void Write(short i)
        {
            Write((byte)((i & 0xFF00) >> 8));
            Write((byte)(i & 0x00FF));
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public override void Write(ushort i)
        {
            Write((byte)((i & 0xFF00) >> 8));
            Write((byte)(i & 0x00FF));
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public override void Write(int i)
        {
            Write((byte)((i & 0xFF000000) >> 24));
            Write((byte)((i & 0x00FF0000) >> 16));
            Write((byte)((i & 0x0000FF00) >> 8));
            Write((byte)(i & 0x000000FF));
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public override void Write(uint i)
        {
            Write((byte)((i & 0xFF000000) >> 24));
            Write((byte)((i & 0x00FF0000) >> 16));
            Write((byte)((i & 0x0000FF00) >> 8));
            Write((byte)(i & 0x000000FF));
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public override void Write(long i)
        {
            var i1 = (uint)(i >> 32);
            var i2 = (uint)i;
            Write(i1);
            Write(i2);
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public override void Write(ulong i)
        {
            var i1 = (uint)(i >> 32);
            var i2 = (uint)i;
            Write(i1);
            Write(i2);
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public override void Write(float f)
        {
            BinaryWriter w = TemporaryBinaryWriter(4);
            w.Write(f);
            byte[] wrongBytes = TemporaryContents(w);
            Write(wrongBytes[3]);
            Write(wrongBytes[2]);
            Write(wrongBytes[1]);
            Write(wrongBytes[0]);
        }

        /// <summary>
        /// Override BinaryWriter's method for network-order.
        /// </summary>
        public override void Write(double d)
        {
            BinaryWriter w = TemporaryBinaryWriter(8);
            w.Write(d);
            byte[] wrongBytes = TemporaryContents(w);
            Write(wrongBytes[7]);
            Write(wrongBytes[6]);
            Write(wrongBytes[5]);
            Write(wrongBytes[4]);
            Write(wrongBytes[3]);
            Write(wrongBytes[2]);
            Write(wrongBytes[1]);
            Write(wrongBytes[0]);
        }
    }
}
