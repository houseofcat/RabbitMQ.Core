using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    internal class TestHeartbeats : IntegrationFixture
    {
        private const ushort heartbeatTimeout = 2;

        [Test, Category("LongRunning"), MaxTimeAttribute(35000)]
        public void TestThatHeartbeatWriterUsesConfigurableInterval()
        {
            var cf = new ConnectionFactory()
            {
                RequestedHeartbeat = heartbeatTimeout,
                AutomaticRecoveryEnabled = false
            };
            RunSingleConnectionTest(cf);
        }

        [Test]
        public void TestThatHeartbeatWriterWithTLSEnabled()
        {
            if (!LongRunningTestsEnabled())
            {
                Console.WriteLine("RABBITMQ_LONG_RUNNING_TESTS is not set, skipping test");
                return;
            }

            var cf = new ConnectionFactory()
            {
                RequestedHeartbeat = heartbeatTimeout,
                AutomaticRecoveryEnabled = false
            };

            string sslDir = IntegrationFixture.CertificatesDirectory();
            if (null == sslDir)
            {
                Console.WriteLine("SSL_CERT_DIR is not configured, skipping test");
                return;
            }
            cf.Ssl.ServerName = System.Net.Dns.GetHostName();
            Assert.IsNotNull(sslDir);
            cf.Ssl.CertPath = sslDir + "/client/keycert.p12";
            string p12Password = Environment.GetEnvironmentVariable("PASSWORD");
            Assert.IsNotNull(p12Password, "missing PASSWORD env var");
            cf.Ssl.CertPassphrase = p12Password;
            cf.Ssl.Enabled = true;

            RunSingleConnectionTest(cf);
        }

        [Test, Category("LongRunning"), MaxTimeAttribute(90000)]
        public void TestHundredsOfConnectionsWithRandomHeartbeatInterval()
        {
            var rnd = new Random();
            List<IConnection> xs = new List<IConnection>();
            for (var i = 0; i < 200; i++)
            {
                var n = Convert.ToUInt16(rnd.Next(2, 6));
                var cf = new ConnectionFactory() { RequestedHeartbeat = n, AutomaticRecoveryEnabled = false };
                var conn = cf.CreateConnection();
                xs.Add(conn);
                var ch = conn.CreateModel();

                conn.ConnectionShutdown += (sender, evt) =>
                    {
                        CheckInitiator(evt);
                    };
            }

            SleepFor(60);

            foreach (var x in xs)
            {
                x.Close();
            }
        }

        protected void RunSingleConnectionTest(ConnectionFactory cf)
        {
            var conn = cf.CreateConnection();
            var ch = conn.CreateModel();
            bool wasShutdown = false;

            conn.ConnectionShutdown += (sender, evt) =>
            {
                lock (conn)
                {
                    if (InitiatedByPeerOrLibrary(evt))
                    {
                        CheckInitiator(evt);
                        wasShutdown = true;
                    }
                }
            };
            SleepFor(30);

            Assert.IsFalse(wasShutdown, "shutdown event should not have been fired");
            Assert.IsTrue(conn.IsOpen, "connection should be open");

            conn.Close();
        }

        private void CheckInitiator(ShutdownEventArgs evt)
        {
            if (InitiatedByPeerOrLibrary(evt))
            {
                Console.WriteLine(((Exception)evt.Cause).StackTrace);
                var s = string.Format("Shutdown: {0}, initiated by: {1}",
                                      evt, evt.Initiator);
                Console.WriteLine(s);
                Assert.Fail(s);
            }
        }

        private bool LongRunningTestsEnabled()
        {
            var s = Environment.GetEnvironmentVariable("RABBITMQ_LONG_RUNNING_TESTS");
            if (s == null || s.Equals(""))
            {
                return false;
            }
            return true;
        }

        private void SleepFor(int t)
        {
            Console.WriteLine("Testing heartbeats, sleeping for {0} seconds", t);
            Thread.Sleep(t * 1000);
        }
    }
}
