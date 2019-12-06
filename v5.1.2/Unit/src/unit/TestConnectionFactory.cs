using NUnit.Framework;
using RabbitMQ.Client.Exceptions;
using System.Collections.Generic;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    internal class TestConnectionFactory
    {
        [Test]
        public void TestProperties()
        {
            var u = "username";
            var pw = "password";
            var v = "vhost";
            var h = "192.168.0.1";
            var p = 5674;

            var cf = new ConnectionFactory
            {
                UserName = u,
                Password = pw,
                VirtualHost = v,
                HostName = h,
                Port = p
            };

            Assert.AreEqual(cf.UserName, u);
            Assert.AreEqual(cf.Password, pw);
            Assert.AreEqual(cf.VirtualHost, v);
            Assert.AreEqual(cf.HostName, h);
            Assert.AreEqual(cf.Port, p);
        }

        [Test]
        public void TestCreateConnectionUsesSpecifiedPort()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true,
                HostName = "localhost",
                Port = 1234
            };
            Assert.That(() =>
                    {
                        using (var conn = cf.CreateConnection()) { }
                    }, Throws.TypeOf<BrokerUnreachableException>());
        }

        [Test]
        public void TestCreateConnectionWithClientProvidedNameUsesSpecifiedPort()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true,
                HostName = "localhost",
                Port = 1234
            };
            Assert.That(() =>
                    {
                        using (var conn = cf.CreateConnection("some_name")) { }
                    }, Throws.TypeOf<BrokerUnreachableException>());
        }

        [Test]
        public void TestCreateConnectionWithClientProvidedNameUsesName()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = false
            };
            using (var conn = cf.CreateConnection("some_name"))
            {
                Assert.AreEqual("some_name", conn.ClientProvidedName);
                Assert.AreEqual("some_name", conn.ClientProperties["connection_name"]);
            }
        }

        [Test]
        public void TestCreateConnectionWithClientProvidedNameAndAutorecoveryUsesName()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true
            };
            using (var conn = cf.CreateConnection("some_name"))
            {
                Assert.AreEqual("some_name", conn.ClientProvidedName);
                Assert.AreEqual("some_name", conn.ClientProperties["connection_name"]);
            }
        }
        [Test]
        public void TestCreateConnectionUsesDefaultPort()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true,
                HostName = "localhost"
            };
            using (var conn = cf.CreateConnection())
            {
                Assert.AreEqual(5672, conn.Endpoint.Port);
            }
        }

        [Test]
        public void TestCreateConnectionWithoutAutoRecoverySelectsAHostFromTheList()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = false,
                HostName = "not_localhost"
            };
            var conn = cf.CreateConnection(new System.Collections.Generic.List<string> { "localhost" }, "oregano");
            conn.Close();
            conn.Dispose();
            Assert.AreEqual("not_localhost", cf.HostName);
            Assert.AreEqual("localhost", conn.Endpoint.HostName);
        }

        [Test]
        public void TestCreateConnectionWithAutoRecoveryUsesAmqpTcpEndpoint()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true,
                HostName = "not_localhost",
                Port = 1234
            };
            var ep = new AmqpTcpEndpoint("localhost");
            using (var conn = cf.CreateConnection(new System.Collections.Generic.List<AmqpTcpEndpoint> { ep })) { }
        }

        [Test]
        public void TestCreateConnectionWithAutoRecoveryUsesInvalidAmqpTcpEndpoint()
        {
            var cf = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true
            };
            var ep = new AmqpTcpEndpoint("localhost", 1234);
            Assert.That(() =>
                    {
                        using (var conn = cf.CreateConnection(new System.Collections.Generic.List<AmqpTcpEndpoint> { ep })) { }
                    }, Throws.TypeOf<BrokerUnreachableException>());
        }

        [Test]
        public void TestCreateConnectionUsesAmqpTcpEndpoint()
        {
            var cf = new ConnectionFactory
            {
                HostName = "not_localhost",
                Port = 1234
            };
            var ep = new AmqpTcpEndpoint("localhost");
            using (var conn = cf.CreateConnection(new System.Collections.Generic.List<AmqpTcpEndpoint> { ep })) { }
        }

        [Test]
        public void TestCreateConnectionWithForcedAddressFamily()
        {
            var cf = new ConnectionFactory
            {
                HostName = "not_localhost"
            };
            var ep = new AmqpTcpEndpoint("localhost")
            {
                AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork
            };
            cf.Endpoint = ep;
            using (var conn = cf.CreateConnection()) { }
        }

        [Test]
        public void TestCreateConnectionUsesInvalidAmqpTcpEndpoint()
        {
            var cf = new ConnectionFactory();
            var ep = new AmqpTcpEndpoint("localhost", 1234);
            Assert.That(() =>
                    {
                        using (var conn = cf.CreateConnection(new System.Collections.Generic.List<AmqpTcpEndpoint> { ep })) { }
                    }, Throws.TypeOf<BrokerUnreachableException>());
        }

        [Test]
        public void TestCreateConnectioUsesValidEndpointWhenMultipleSupplied()
        {
            var cf = new ConnectionFactory();
            var invalidEp = new AmqpTcpEndpoint("not_localhost");
            var ep = new AmqpTcpEndpoint("localhost");
            using (var conn = cf.CreateConnection(new List<AmqpTcpEndpoint> { invalidEp, ep })) { }
        }
    }
}
