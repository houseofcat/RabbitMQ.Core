using NUnit.Framework;
using System;
using System.Net.Security;
using System.Security.Authentication;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestSsl
    {
        public void SendReceive(ConnectionFactory cf)
        {
            using (IConnection conn = cf.CreateConnection())
            {
                IModel ch = conn.CreateModel();

                ch.ExchangeDeclare("Exchange_TestSslEndPoint", ExchangeType.Direct);
                string qName = ch.QueueDeclare();
                ch.QueueBind(qName, "Exchange_TestSslEndPoint", "Key_TestSslEndpoint", null);

                string message = "Hello C# SSL Client World";
                byte[] msgBytes = System.Text.Encoding.UTF8.GetBytes(message);
                ch.BasicPublish("Exchange_TestSslEndPoint", "Key_TestSslEndpoint", null, msgBytes);

                bool autoAck = false;
                BasicGetResult result = ch.BasicGet(qName, autoAck);
                byte[] body = result.Body.ToArray();
                string resultMessage = System.Text.Encoding.UTF8.GetString(body);

                Assert.AreEqual(message, resultMessage);
            }
        }

        [Test]
        public void TestServerVerifiedIgnoringNameMismatch()
        {
            string sslDir = IntegrationFixture.CertificatesDirectory();
            if (null == sslDir)
            {
                Console.WriteLine("SSL_CERT_DIR is not configured, skipping test");
                return;
            }

            ConnectionFactory cf = new ConnectionFactory();
            cf.Ssl.ServerName = "*";
            cf.Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch;
            cf.Ssl.Enabled = true;
            SendReceive(cf);
        }

        [Test]
        public void TestServerVerified()
        {
            string sslDir = IntegrationFixture.CertificatesDirectory();
            if (null == sslDir)
            {
                Console.WriteLine("SSL_CERT_DIR is not configured, skipping test");
                return;
            }

            ConnectionFactory cf = new ConnectionFactory();
            cf.Ssl.ServerName = System.Net.Dns.GetHostName();
            cf.Ssl.Enabled = true;
            SendReceive(cf);
        }

        [Test]
        public void TestClientAndServerVerified()
        {
            string sslDir = IntegrationFixture.CertificatesDirectory();
            if (null == sslDir)
            {
                Console.WriteLine("SSL_CERT_DIR is not configured, skipping test");
                return;
            }

            ConnectionFactory cf = new ConnectionFactory();
            cf.Ssl.ServerName = System.Net.Dns.GetHostName();
            Assert.IsNotNull(sslDir);
            cf.Ssl.CertPath = sslDir + "/client/keycert.p12";
            string p12Password = Environment.GetEnvironmentVariable("PASSWORD");
            Assert.IsNotNull(p12Password, "missing PASSWORD env var");
            cf.Ssl.CertPassphrase = p12Password;
            cf.Ssl.Enabled = true;
            SendReceive(cf);
        }

        // rabbitmq/rabbitmq-dotnet-client#46, also #44 and #45
        [Test]
        public void TestNoClientCertificate()
        {
            string sslDir = IntegrationFixture.CertificatesDirectory();
            if (null == sslDir)
            {
                Console.WriteLine("SSL_CERT_DIR is not configured, skipping test");
                return;
            }

            ConnectionFactory cf = new ConnectionFactory
            {
                Ssl = new SslOption()
                {
                    CertPath = null,
                    Enabled = true,
                }
            };

            cf.Ssl.Version = SslProtocols.None;
            cf.Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNotAvailable |
                                        SslPolicyErrors.RemoteCertificateNameMismatch;

            SendReceive(cf);
        }
    }
}
