using NUnit.Framework;
using System;
using System.Text;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestUpdateSecret : IntegrationFixture
    {
        [Test]
        public void TestUpdatingConnectionSecret()
        {
            if (!RabbitMQ380OrHigher())
            {
                Console.WriteLine("Not connected to RabbitMQ 3.8 or higher, skipping test");
                return;
            }

            Conn.UpdateSecret("new-secret", "Test Case");

            Assert.AreEqual("new-secret", ConnFactory.Password);
        }

        private bool RabbitMQ380OrHigher()
        {
            System.Collections.Generic.IDictionary<string, object> properties = Conn.ServerProperties;

            if (properties.TryGetValue("version", out object versionVal))
            {
                string versionStr = Encoding.UTF8.GetString((byte[])versionVal);
                if (Version.TryParse(versionStr, out Version version))
                {
                    return version >= new Version(3, 8);
                }
            }

            return false;
        }
    }
}
