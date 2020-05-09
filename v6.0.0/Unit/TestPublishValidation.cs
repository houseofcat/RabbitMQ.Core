using NUnit.Framework;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestPublishValidation : IntegrationFixture
    {
        [Test]
        public void TestNullRoutingKeyIsRejected()
        {
            IModel ch = Conn.CreateModel();
            Assert.Throws(typeof(ArgumentNullException), () => ch.BasicPublish("", null, null, encoding.GetBytes("msg")));
        }
    }
}
