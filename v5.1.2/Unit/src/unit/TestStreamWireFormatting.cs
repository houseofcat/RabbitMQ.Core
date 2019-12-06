using NUnit.Framework;
using RabbitMQ.Client.Content;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestStreamWireFormatting : WireFormattingFixture
    {
        [Test]
        public void TestSingleDecoding1()
        {
            Assert.AreEqual(1.234f,
                            StreamWireFormatting.ReadSingle(Reader
                                                            (new byte[] { 8, 63, 157, 243, 182 })));
        }

        [Test]
        public void TestSingleDecoding2()
        {
            Assert.AreEqual(1.234f.ToString(),
                            StreamWireFormatting.ReadString(Reader
                                                            (new byte[] { 8, 63, 157, 243, 182 })));
        }

        [Test]
        public void TestRoundTrip()
        {
            NetworkBinaryWriter w = Writer();
            StreamWireFormatting.WriteBool(w, true);
            StreamWireFormatting.WriteInt32(w, 1234);
            StreamWireFormatting.WriteInt16(w, 1234);
            StreamWireFormatting.WriteByte(w, 123);
            StreamWireFormatting.WriteChar(w, 'x');
            StreamWireFormatting.WriteInt64(w, 1234);
            StreamWireFormatting.WriteSingle(w, 1.234f);
            StreamWireFormatting.WriteDouble(w, 1.234);
            StreamWireFormatting.WriteBytes(w, new byte[] { 1, 2, 3, 4 });
            StreamWireFormatting.WriteString(w, "hello");
            StreamWireFormatting.WriteObject(w, "world");
            NetworkBinaryReader r = Reader(Contents(w));
            Assert.AreEqual(true, StreamWireFormatting.ReadBool(r));
            Assert.AreEqual(1234, StreamWireFormatting.ReadInt32(r));
            Assert.AreEqual(1234, StreamWireFormatting.ReadInt16(r));
            Assert.AreEqual(123, StreamWireFormatting.ReadByte(r));
            Assert.AreEqual('x', StreamWireFormatting.ReadChar(r));
            Assert.AreEqual(1234, StreamWireFormatting.ReadInt64(r));
            Assert.AreEqual(1.234f, StreamWireFormatting.ReadSingle(r));
            Assert.AreEqual(1.234, StreamWireFormatting.ReadDouble(r));
            Assert.AreEqual(new byte[] { 1, 2, 3, 4 }, StreamWireFormatting.ReadBytes(r));
            Assert.AreEqual("hello", StreamWireFormatting.ReadString(r));
            Assert.AreEqual("world", StreamWireFormatting.ReadObject(r));
        }
    }
}
