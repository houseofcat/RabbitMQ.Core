using NUnit.Framework;
using RabbitMQ.Client.Content;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestBytesWireFormatting : WireFormattingFixture
    {
        [Test]
        public void TestSingleDecoding()
        {
            Assert.AreEqual(1.234f,
                            BytesWireFormatting.ReadSingle(Reader
                                                           (new byte[] { 63, 157, 243, 182 })));
        }

        [Test]
        public void TestSingleEncoding()
        {
            NetworkBinaryWriter w = Writer();
            BytesWireFormatting.WriteSingle(w, 1.234f);
            Check(w, new byte[] { 63, 157, 243, 182 });
        }

        [Test]
        public void TestDoubleDecoding()
        {
            Assert.AreEqual(1.234,
                            BytesWireFormatting.ReadDouble(Reader
                                                           (new byte[] { 63, 243, 190, 118,
                                                                         200, 180, 57, 88 })));
        }

        [Test]
        public void TestDoubleEncoding()
        {
            NetworkBinaryWriter w = Writer();
            BytesWireFormatting.WriteDouble(w, 1.234);
            Check(w, new byte[] { 63, 243, 190, 118, 200, 180, 57, 88 });
        }
    }
}
