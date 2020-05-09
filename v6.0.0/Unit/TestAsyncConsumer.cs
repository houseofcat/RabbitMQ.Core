using NUnit.Framework;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestAsyncConsumer
    {
        [Test]
        public void TestBasicRoundtrip()
        {
            var cf = new ConnectionFactory { DispatchConsumersAsync = true };
            using IConnection c = cf.CreateConnection();
            using IModel m = c.CreateModel();
            QueueDeclareOk q = m.QueueDeclare();
            IBasicProperties bp = m.CreateBasicProperties();
            byte[] body = System.Text.Encoding.UTF8.GetBytes("async-hi");
            m.BasicPublish("", q.QueueName, bp, body);
            var consumer = new AsyncEventingBasicConsumer(m);
            var are = new AutoResetEvent(false);
            consumer.Received += async (o, a) =>
                {
                    are.Set();
                    await Task.Yield();
                };
            string tag = m.BasicConsume(q.QueueName, true, consumer);
            // ensure we get a delivery
            bool waitRes = are.WaitOne(2000);
            Assert.IsTrue(waitRes);
            // unsubscribe and ensure no further deliveries
            m.BasicCancel(tag);
            m.BasicPublish("", q.QueueName, bp, body);
            bool waitResFalse = are.WaitOne(2000);
            Assert.IsFalse(waitResFalse);
        }

        [Test]
        public void TestBasicRoundtripNoWait()
        {
            var cf = new ConnectionFactory { DispatchConsumersAsync = true };
            using IConnection c = cf.CreateConnection();
            using IModel m = c.CreateModel();
            QueueDeclareOk q = m.QueueDeclare();
            IBasicProperties bp = m.CreateBasicProperties();
            byte[] body = System.Text.Encoding.UTF8.GetBytes("async-hi");
            m.BasicPublish("", q.QueueName, bp, body);
            var consumer = new AsyncEventingBasicConsumer(m);
            var are = new AutoResetEvent(false);
            consumer.Received += async (o, a) =>
                {
                    are.Set();
                    await Task.Yield();
                };
            string tag = m.BasicConsume(q.QueueName, true, consumer);
            // ensure we get a delivery
            bool waitRes = are.WaitOne(2000);
            Assert.IsTrue(waitRes);
            // unsubscribe and ensure no further deliveries
            m.BasicCancelNoWait(tag);
            m.BasicPublish("", q.QueueName, bp, body);
            bool waitResFalse = are.WaitOne(2000);
            Assert.IsFalse(waitResFalse);
        }

        [Test]
        public void NonAsyncConsumerShouldThrowInvalidOperationException()
        {
            var cf = new ConnectionFactory { DispatchConsumersAsync = true };
            using IConnection c = cf.CreateConnection();
            using IModel m = c.CreateModel();
            QueueDeclareOk q = m.QueueDeclare();
            IBasicProperties bp = m.CreateBasicProperties();
            byte[] body = System.Text.Encoding.UTF8.GetBytes("async-hi");
            m.BasicPublish("", q.QueueName, bp, body);
            var consumer = new EventingBasicConsumer(m);
            Assert.Throws<InvalidOperationException>(() => m.BasicConsume(q.QueueName, false, consumer));
        }
    }
}
