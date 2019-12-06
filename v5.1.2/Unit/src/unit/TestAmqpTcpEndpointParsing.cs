using NUnit.Framework;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestAmqpTcpEndpointParsing
    {
        [Test]
        public void TestHostWithPort()
        {
            AmqpTcpEndpoint e = AmqpTcpEndpoint.Parse("host:1234");

            Assert.AreEqual("host", e.HostName);
            Assert.AreEqual(1234, e.Port);
        }

        [Test]
        public void TestHostWithoutPort()
        {
            AmqpTcpEndpoint e = AmqpTcpEndpoint.Parse("host");

            Assert.AreEqual("host", e.HostName);
            Assert.AreEqual(Protocols.DefaultProtocol.DefaultPort, e.Port);
        }

        [Test]
        public void TestEmptyHostWithPort()
        {
            AmqpTcpEndpoint e = AmqpTcpEndpoint.Parse(":1234");

            Assert.AreEqual("", e.HostName);
            Assert.AreEqual(1234, e.Port);
        }

        [Test]
        public void TestEmptyHostWithoutPort()
        {
            AmqpTcpEndpoint e = AmqpTcpEndpoint.Parse(":");

            Assert.AreEqual("", e.HostName);
            Assert.AreEqual(Protocols.DefaultProtocol.DefaultPort, e.Port);
        }

        [Test]
        public void TestCompletelyEmptyString()
        {
            AmqpTcpEndpoint e = AmqpTcpEndpoint.Parse("");

            Assert.AreEqual("", e.HostName);
            Assert.AreEqual(Protocols.DefaultProtocol.DefaultPort, e.Port);
        }

        [Test]
        public void TestInvalidPort()
        {
            try
            {
                AmqpTcpEndpoint.Parse("host:port");
                Assert.Fail("Expected FormatException");
            }
            catch (FormatException)
            {
                // OK.
            }
        }

        [Test]
        public void TestMultipleNone()
        {
            AmqpTcpEndpoint[] es = AmqpTcpEndpoint.ParseMultiple("  ");
            Assert.AreEqual(0, es.Length);
        }

        [Test]
        public void TestMultipleOne()
        {
            AmqpTcpEndpoint[] es = AmqpTcpEndpoint.ParseMultiple(" host:1234 ");
            Assert.AreEqual(1, es.Length);
            Assert.AreEqual("host", es[0].HostName);
            Assert.AreEqual(1234, es[0].Port);
        }

        [Test]
        public void TestMultipleTwo()
        {
            AmqpTcpEndpoint[] es = AmqpTcpEndpoint.ParseMultiple(" host:1234, other:2345 ");
            Assert.AreEqual(2, es.Length);
            Assert.AreEqual("host", es[0].HostName);
            Assert.AreEqual(1234, es[0].Port);
            Assert.AreEqual("other", es[1].HostName);
            Assert.AreEqual(2345, es[1].Port);
        }

        [Test]
        public void TestMultipleTwoMultipleCommas()
        {
            AmqpTcpEndpoint[] es = AmqpTcpEndpoint.ParseMultiple(", host:1234,, ,,, other:2345,, ");
            Assert.AreEqual(2, es.Length);
            Assert.AreEqual("host", es[0].HostName);
            Assert.AreEqual(1234, es[0].Port);
            Assert.AreEqual("other", es[1].HostName);
            Assert.AreEqual(2345, es[1].Port);
        }

        [Test]
        public void TestIpv6WithPort()
        {
            AmqpTcpEndpoint e = AmqpTcpEndpoint.Parse("[::1]:1234");

            Assert.AreEqual("::1", e.HostName);
            Assert.AreEqual(1234, e.Port);
        }

        [Test]
        public void TestIpv6WithoutPort()
        {
            AmqpTcpEndpoint e = AmqpTcpEndpoint.Parse("[::1]");

            Assert.AreEqual("::1", e.HostName);
            Assert.AreEqual(Protocols.DefaultProtocol.DefaultPort, e.Port);
        }
    }
}
