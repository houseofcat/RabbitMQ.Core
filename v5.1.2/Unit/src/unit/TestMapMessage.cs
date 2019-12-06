using NUnit.Framework;
using RabbitMQ.Client.Content;
using RabbitMQ.Util;
using System.Collections.Generic;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestMapMessage : WireFormattingFixture
    {
        [Test]
        public void TestRoundTrip()
        {
            NetworkBinaryWriter w = Writer();
            Dictionary<string, object> t = new Dictionary<string, object>();
            t["double"] = 1.234;
            t["string"] = "hello";
            MapWireFormatting.WriteMap(w, t);
            IDictionary<string, object> t2 = MapWireFormatting.ReadMap(Reader(Contents(w)));
            Assert.AreEqual(2, t2.Count);
            Assert.AreEqual(1.234, t2["double"]);
            Assert.AreEqual("hello", t2["string"]);
        }

        [Test]
        public void TestEncoding()
        {
            NetworkBinaryWriter w = Writer();
            Dictionary<string, object> t = new Dictionary<string, object>();
            t["double"] = 1.234;
            t["string"] = "hello";
            MapWireFormatting.WriteMap(w, t);
            Check(w, new byte[] {
                0x00, 0x00, 0x00, 0x02,

                0x64, 0x6F, 0x75, 0x62, 0x6C, 0x65, 0x00,
                0x09,
                0x3F, 0xF3, 0xBE, 0x76, 0xC8, 0xB4, 0x39, 0x58,

                0x73, 0x74, 0x72, 0x69, 0x6E, 0x67, 0x00,
                0x0A,
                0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x00
            });
        }
    }
}
