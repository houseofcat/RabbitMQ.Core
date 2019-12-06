using NUnit.Framework;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System;
using System.IO;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestContentHeaderCodec
    {
        public static ContentHeaderPropertyWriter Writer()
        {
            return new ContentHeaderPropertyWriter(new NetworkBinaryWriter(new MemoryStream()));
        }

        public static ContentHeaderPropertyReader Reader(byte[] bytes)
        {
            return new ContentHeaderPropertyReader(new NetworkBinaryReader(new MemoryStream(bytes)));
        }

        public byte[] Contents(ContentHeaderPropertyWriter w)
        {
            return ((MemoryStream)w.BaseWriter.BaseStream).ToArray();
        }

        public void Check(ContentHeaderPropertyWriter w, byte[] expected)
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

        public ContentHeaderPropertyWriter m_w;

        [SetUp]
        public void SetUp()
        {
            m_w = Writer();
        }

        [Test]
        public void TestPresence()
        {
            m_w.WritePresence(false);
            m_w.WritePresence(true);
            m_w.WritePresence(false);
            m_w.WritePresence(true);
            m_w.FinishPresence();
            Check(m_w, new byte[] { 0x50, 0x00 });
        }

        [Test]
        public void TestLongPresence()
        {
            m_w.WritePresence(false);
            m_w.WritePresence(true);
            m_w.WritePresence(false);
            m_w.WritePresence(true);
            for (int i = 0; i < 20; i++)
            {
                m_w.WritePresence(false);
            }
            m_w.WritePresence(true);
            m_w.FinishPresence();
            Check(m_w, new byte[] { 0x50, 0x01, 0x00, 0x40 });
        }

        [Test]
        public void TestNoPresence()
        {
            m_w.FinishPresence();
            Check(m_w, new byte[] { 0x00, 0x00 });
        }

        [Test]
        public void TestBodyLength()
        {
            RabbitMQ.Client.Framing.BasicProperties prop =
                new RabbitMQ.Client.Framing.BasicProperties();
            prop.WriteTo(m_w.BaseWriter, 0x123456789ABCDEF0UL);
            Check(m_w, new byte[] { 0x00, 0x00, // weight
			          0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, // body len
			          0x00, 0x00}); // props flags
        }

        [Test]
        public void TestSimpleProperties()
        {
            RabbitMQ.Client.Framing.BasicProperties prop =
                new RabbitMQ.Client.Framing.BasicProperties
                {
                    ContentType = "text/plain"
                };
            prop.WriteTo(m_w.BaseWriter, 0x123456789ABCDEF0UL);
            Check(m_w, new byte[] { 0x00, 0x00, // weight
			          0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, // body len
			          0x80, 0x00, // props flags
			          0x0A, // shortstr len
			          0x74, 0x65, 0x78, 0x74,
                      0x2F, 0x70, 0x6C, 0x61,
                      0x69, 0x6E });
        }
    }
}
