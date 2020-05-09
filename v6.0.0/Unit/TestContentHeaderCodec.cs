using NUnit.Framework;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    class TestContentHeaderCodec
    {
        public void Check(ReadOnlyMemory<byte> actual, ReadOnlyMemory<byte> expected)
        {
            try
            {
                Assert.AreEqual(expected.ToArray(), actual.ToArray());
            }
            catch
            {
                Console.WriteLine();
                Console.WriteLine("EXPECTED ==================================================");
                DebugUtil.Dump(expected.ToArray(), Console.Out);
                Console.WriteLine("ACTUAL ====================================================");
                DebugUtil.Dump(actual.ToArray(), Console.Out);
                Console.WriteLine("===========================================================");
                throw;
            }
        }

        [Test]
        public void TestPresence()
        {
            var m_w = new ContentHeaderPropertyWriter(new byte[1024]);
            m_w.WritePresence(false);
            m_w.WritePresence(true);
            m_w.WritePresence(false);
            m_w.WritePresence(true);
            m_w.FinishPresence();
            Check(m_w.Memory.Slice(0, m_w.Offset), new byte[] { 0x50, 0x00 });
        }

        [Test]
        public void TestLongPresence()
        {
            var m_w = new ContentHeaderPropertyWriter(new byte[1024]);

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
            Check(m_w.Memory.Slice(0, m_w.Offset), new byte[] { 0x50, 0x01, 0x00, 0x40 });
        }

        [Test]
        public void TestNoPresence()
        {
            var m_w = new ContentHeaderPropertyWriter(new byte[1024]);
            m_w.FinishPresence();
            Check(m_w.Memory.Slice(0, m_w.Offset), new byte[] { 0x00, 0x00 });
        }

        [Test]
        public void TestBodyLength()
        {
            RabbitMQ.Client.Framing.BasicProperties prop =
                new RabbitMQ.Client.Framing.BasicProperties();
            int bytesNeeded = prop.GetRequiredBufferSize();
            byte[] bytes = new byte[bytesNeeded];
            int bytesWritten = prop.WriteTo(bytes, 0x123456789ABCDEF0UL);
            prop.WriteTo(bytes, 0x123456789ABCDEF0UL);
            Check(bytes.AsMemory().Slice(0, bytesWritten), new byte[] { 0x00, 0x00, // weight
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
            int bytesNeeded = prop.GetRequiredBufferSize();
            byte[] bytes = new byte[bytesNeeded];
            int bytesWritten = prop.WriteTo(bytes, 0x123456789ABCDEF0UL);
            Check(bytes.AsMemory().Slice(0, bytesWritten), new byte[] { 0x00, 0x00, // weight
			          0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, // body len
			          0x80, 0x00, // props flags
			          0x0A, // shortstr len
			          0x74, 0x65, 0x78, 0x74,
                      0x2F, 0x70, 0x6C, 0x61,
                      0x69, 0x6E });
        }
    }
}
