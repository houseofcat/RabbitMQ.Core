using NUnit.Framework;
using RabbitMQ.Client.Events;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestMainLoop : IntegrationFixture
    {
        private class FaultyConsumer : DefaultBasicConsumer
        {
            public FaultyConsumer(IModel model) : base(model) { }

            public override void HandleBasicDeliver(string consumerTag,
                                               ulong deliveryTag,
                                               bool redelivered,
                                               string exchange,
                                               string routingKey,
                                               IBasicProperties properties,
                                               byte[] body)
            {
                throw new Exception("I am a bad consumer");
            }
        }

        [Test]
        public void TestCloseWithFaultyConsumer()
        {
            ConnectionFactory connFactory = new ConnectionFactory();
            IConnection c = connFactory.CreateConnection();
            IModel m = Conn.CreateModel();
            object o = new object();
            string q = GenerateQueueName();
            m.QueueDeclare(q, false, false, false, null);

            CallbackExceptionEventArgs ea = null;
            m.CallbackException += (_, evt) =>
            {
                ea = evt;
                c.Close();
                Monitor.PulseAll(o);
            };
            m.BasicConsume(q, true, new FaultyConsumer(Model));
            m.BasicPublish("", q, null, encoding.GetBytes("message"));
            WaitOn(o);

            Assert.IsNotNull(ea);
            Assert.AreEqual(c.IsOpen, false);
            Assert.AreEqual(c.CloseReason.ReplyCode, 200);
        }
    }
}
