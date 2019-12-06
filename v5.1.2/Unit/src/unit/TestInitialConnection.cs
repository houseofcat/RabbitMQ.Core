using NUnit.Framework;
using RabbitMQ.Client.Exceptions;
using System.Collections.Generic;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestInitialConnection : IntegrationFixture
    {
        [Test]
        public void TestBasicConnectionRecoveryWithHostnameList()
        {
            var c = CreateAutorecoveringConnection(new List<string>() { "127.0.0.1", "localhost" });
            Assert.IsTrue(c.IsOpen);
            c.Close();
        }

        [Test]
        public void TestBasicConnectionRecoveryWithHostnameListAndUnreachableHosts()
        {
            var c = CreateAutorecoveringConnection(new List<string>() { "191.72.44.22", "127.0.0.1", "localhost" });
            Assert.IsTrue(c.IsOpen);
            c.Close();
        }

        [Test]
        public void TestBasicConnectionRecoveryWithHostnameListWithOnlyUnreachableHosts()
        {
            Assert.Throws<BrokerUnreachableException>(() =>
            {
                CreateAutorecoveringConnection(new List<string>() {
                    "191.72.44.22",
                    "145.23.22.18",
                    "192.255.255.255"
                });
            });
        }
    }
}
