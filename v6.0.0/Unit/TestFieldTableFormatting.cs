// This source code is dual-licensed under the Apache License, version
using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System.Collections;
using System.Text;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    class TestFieldTableFormatting : WireFormattingFixture
    {
        [Test]
        public void TestStandardTypes()
        {
            Hashtable t = new Hashtable
            {
                ["string"] = "Hello",
                ["int"] = 1234,
                ["uint"] = 1234u,
                ["decimal"] = 12.34m,
                ["timestamp"] = new AmqpTimestamp(0)
            };
            Hashtable t2 = new Hashtable();
            t["fieldtable"] = t2;
            t2["test"] = "test";
            IList array = new ArrayList
            {
                "longstring",
                1234
            };
            t["fieldarray"] = array;
            int bytesNeeded = WireFormatting.GetTableByteCount(t);
            byte[] bytes = new byte[bytesNeeded];
            WireFormatting.WriteTable(bytes, t);
            IDictionary nt = (IDictionary)WireFormatting.ReadTable(bytes, out int bytesRead);
            Assert.AreEqual(bytesNeeded, bytesRead);
            Assert.AreEqual(Encoding.UTF8.GetBytes("Hello"), nt["string"]);
            Assert.AreEqual(1234, nt["int"]);
            Assert.AreEqual(1234u, nt["uint"]);
            Assert.AreEqual(12.34m, nt["decimal"]);
            Assert.AreEqual(0, ((AmqpTimestamp)nt["timestamp"]).UnixTime);
            IDictionary nt2 = (IDictionary)nt["fieldtable"];
            Assert.AreEqual(Encoding.UTF8.GetBytes("test"), nt2["test"]);
            IList narray = (IList)nt["fieldarray"];
            Assert.AreEqual(Encoding.UTF8.GetBytes("longstring"), narray[0]);
            Assert.AreEqual(1234, narray[1]);
        }

        [Test]
        public void TestTableEncoding_S()
        {
            Hashtable t = new Hashtable
            {
                ["a"] = "bc"
            };
            int bytesNeeded = WireFormatting.GetTableByteCount(t);
            byte[] bytes = new byte[bytesNeeded];
            WireFormatting.WriteTable(bytes, t);
            WireFormatting.ReadTable(bytes, out int bytesRead);
            Assert.AreEqual(bytesNeeded, bytesRead);
            Check(bytes, new byte[] {
                    0,0,0,9, // table length
                    1,(byte)'a', // key
                    (byte)'S', // type
                    0,0,0,2,(byte)'b',(byte)'c' // value
                });
        }

        [Test]
        public void TestTableEncoding_x()
        {
            Hashtable t = new Hashtable
            {
                ["a"] = new BinaryTableValue(new byte[] { 0xaa, 0x55 })
            };
            int bytesNeeded = WireFormatting.GetTableByteCount(t);
            byte[] bytes = new byte[bytesNeeded];
            WireFormatting.WriteTable(bytes, t);
            WireFormatting.ReadTable(bytes, out int bytesRead);
            Assert.AreEqual(bytesNeeded, bytesRead);
            Check(bytes, new byte[] {
                    0,0,0,9, // table length
                    1,(byte)'a', // key
                    (byte)'x', // type
                    0,0,0,2,0xaa,0x55 // value
                });
        }

        [Test]
        public void TestQpidJmsTypes()
        {
            Hashtable t = new Hashtable // 4
            {
                ["B"] = (byte)255, // 2+2
                ["b"] = (sbyte)-128, // 2+2
                ["d"] = (double)123, // 2+9
                ["f"] = (float)123,  // 2+5
                ["l"] = (long)123, // 2+9
                ["s"] = (short)123, // 2+2
                ["t"] = true // 2+2
            };
            byte[] xbytes = { 0xaa, 0x55 };
            t["x"] = new BinaryTableValue(xbytes); // 2+5+2
            t["V"] = null; // 2+1
            int bytesNeeded = WireFormatting.GetTableByteCount(t);
            byte[] bytes = new byte[bytesNeeded];
            WireFormatting.WriteTable(bytes, t);
            IDictionary nt = (IDictionary)WireFormatting.ReadTable(bytes, out int bytesRead);
            Assert.AreEqual(bytesNeeded, bytesRead);
            Assert.AreEqual(typeof(byte), nt["B"].GetType()); Assert.AreEqual((byte)255, nt["B"]);
            Assert.AreEqual(typeof(sbyte), nt["b"].GetType()); Assert.AreEqual((sbyte)-128, nt["b"]);
            Assert.AreEqual(typeof(double), nt["d"].GetType()); Assert.AreEqual((double)123, nt["d"]);
            Assert.AreEqual(typeof(float), nt["f"].GetType()); Assert.AreEqual((float)123, nt["f"]);
            Assert.AreEqual(typeof(long), nt["l"].GetType()); Assert.AreEqual((long)123, nt["l"]);
            Assert.AreEqual(typeof(short), nt["s"].GetType()); Assert.AreEqual((short)123, nt["s"]);
            Assert.AreEqual(true, nt["t"]);
            Assert.AreEqual(xbytes, ((BinaryTableValue)nt["x"]).Bytes);
            Assert.AreEqual(null, nt["V"]);
        }
    }
}
