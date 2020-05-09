using NUnit.Framework;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System;
using System.Collections;
using System.Text;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    class TestMethodArgumentCodec
    {
        public static MethodArgumentWriter Writer()
        {
            return new MethodArgumentWriter(new byte[1024]);
        }

        public static MethodArgumentReader Reader(byte[] bytes)
        {
            return new MethodArgumentReader(bytes);
        }

        public byte[] Contents(MethodArgumentWriter w)
        {
            return w.Memory.Slice(0, w.Offset).ToArray();
        }

        public void Check(MethodArgumentWriter w, byte[] expected)
        {
            byte[] actual = Contents(w);
            try
            {
                Assert.AreEqual(expected, actual);
            }
            catch
            {
                Console.WriteLine();
                Console.WriteLine("EXPECTED ==================================================");
                DebugUtil.Dump(expected, Console.Out);
                Console.WriteLine("ACTUAL ====================================================");
                DebugUtil.Dump(actual, Console.Out);
                Console.WriteLine("===========================================================");
                throw;
            }
        }

        [Test]
        public void TestTableLengthWrite()
        {
            var t = new Hashtable
            {
                ["abc"] = "def"
            };

            int bytesNeeded = WireFormatting.GetTableByteCount(t);
            var writer = new MethodArgumentWriter(new byte[bytesNeeded]);
            writer.WriteTable(t);
            Assert.AreEqual(bytesNeeded, writer.Offset);
            Check(writer, new byte[] { 0x00, 0x00, 0x00, 0x0C,
                                   0x03, 0x61, 0x62, 0x63,
                                   0x53, 0x00, 0x00, 0x00,
                                   0x03, 0x64, 0x65, 0x66 });
        }

        [Test]
        public void TestTableLengthRead()
        {
            IDictionary t = (IDictionary)Reader(new byte[] { 0x00, 0x00, 0x00, 0x0C,
                                                             0x03, 0x61, 0x62, 0x63,
                                                             0x53, 0x00, 0x00, 0x00,
                                                             0x03, 0x64, 0x65, 0x66 }).ReadTable();
            Assert.AreEqual(Encoding.UTF8.GetBytes("def"), t["abc"]);
            Assert.AreEqual(1, t.Count);
        }

        [Test]
        public void TestNestedTableWrite()
        {
            Hashtable t = new Hashtable();
            Hashtable x = new Hashtable
            {
                ["y"] = 0x12345678
            };
            t["x"] = x;
            int bytesNeeded = WireFormatting.GetTableByteCount(t);
            var writer = new MethodArgumentWriter(new byte[bytesNeeded]);
            writer.WriteTable(t);
            Assert.AreEqual(bytesNeeded, writer.Offset);
            Check(writer, new byte[] { 0x00, 0x00, 0x00, 0x0E,
                                   0x01, 0x78, 0x46, 0x00,
                                   0x00, 0x00, 0x07, 0x01,
                                   0x79, 0x49, 0x12, 0x34,
                                   0x56, 0x78 });
        }
    }
}
