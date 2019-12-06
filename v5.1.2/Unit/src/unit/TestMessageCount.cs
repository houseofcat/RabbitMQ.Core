using NUnit.Framework;
using System;

namespace RabbitMQ.Client.Unit
{
    internal class TestMessageCount : IntegrationFixture
    {
        [Test]
        public void TestMessageCountMethod()
        {
            Model.ConfirmSelect();
            var q = GenerateQueueName();
            Model.QueueDeclare(queue: q, durable: false, exclusive: true, autoDelete: false, arguments: null);
            Assert.AreEqual(0, Model.MessageCount(q));

            Model.BasicPublish(exchange: "", routingKey: q, basicProperties: null, body: encoding.GetBytes("msg"));
            Model.WaitForConfirms(TimeSpan.FromSeconds(2));
            Assert.AreEqual(1, Model.MessageCount(q));
        }
    }
}
