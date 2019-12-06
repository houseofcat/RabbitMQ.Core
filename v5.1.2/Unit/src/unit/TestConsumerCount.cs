using NUnit.Framework;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.Unit
{
    internal class TestConsumerCount : IntegrationFixture
    {
        [Test]
        public void TestConsumerCountMethod()
        {
            var q = GenerateQueueName();
            Model.QueueDeclare(queue: q, durable: false, exclusive: true, autoDelete: false, arguments: null);
            Assert.AreEqual(0, Model.ConsumerCount(q));

            var tag = Model.BasicConsume(q, true, new EventingBasicConsumer(Model));
            Assert.AreEqual(1, Model.ConsumerCount(q));

            Model.BasicCancel(tag);
            Assert.AreEqual(0, Model.ConsumerCount(q));
        }
    }
}
