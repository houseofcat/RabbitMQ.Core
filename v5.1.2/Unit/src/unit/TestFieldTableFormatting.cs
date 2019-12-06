using NUnit.Framework;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System.Collections;
using System.Text;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestFieldTableFormatting : WireFormattingFixture
    {
        [Test]
        public void TestStandardTypes()
        {
            NetworkBinaryWriter w = Writer();
            Hashtable t = new Hashtable();
            t["string"] = "Hello";
            t["int"] = 1234;
            t["uint"] = 1234u;
            t["decimal"] = 12.34m;
            t["timestamp"] = new AmqpTimestamp(0);
            Hashtable t2 = new Hashtable();
            t["fieldtable"] = t2;
            t2["test"] = "test";
            IList array = new ArrayList();
            array.Add("longstring");
            array.Add(1234);
            t["fieldarray"] = array;
            WireFormatting.WriteTable(w, t);
            IDictionary nt = (IDictionary)WireFormatting.ReadTable(Reader(Contents(w)));
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
            NetworkBinaryWriter w = Writer();
            Hashtable t = new Hashtable();
            t["a"] = "bc";
            WireFormatting.WriteTable(w, t);
            Check(w, new byte[] {
                    0,0,0,9, // table length
                    1,(byte)'a', // key
                    (byte)'S', // type
                    0,0,0,2,(byte)'b',(byte)'c' // value
                });
        }

        [Test]
        public void TestTableEncoding_x()
        {
            NetworkBinaryWriter w = Writer();
            Hashtable t = new Hashtable();
            t["a"] = new BinaryTableValue(new byte[] { 0xaa, 0x55 });
            WireFormatting.WriteTable(w, t);
            Check(w, new byte[] {
                    0,0,0,9, // table length
                    1,(byte)'a', // key
                    (byte)'x', // type
                    0,0,0,2,0xaa,0x55 // value
                });
        }

        [Test]
        public void TestQpidJmsTypes()
        {
            NetworkBinaryWriter w = Writer();
            Hashtable t = new Hashtable();
            t["B"] = (byte)255;
            t["b"] = (sbyte)-128;
            t["d"] = (double)123;
            t["f"] = (float)123;
            t["l"] = (long)123;
            t["s"] = (short)123;
            t["t"] = true;
            byte[] xbytes = { 0xaa, 0x55 };
            t["x"] = new BinaryTableValue(xbytes);
            t["V"] = null;
            WireFormatting.WriteTable(w, t);
            IDictionary nt = (IDictionary)WireFormatting.ReadTable(Reader(Contents(w)));
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
