using NUnit.Framework;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestConsumerExceptions : IntegrationFixture
    {
        private class ConsumerFailingOnDelivery : DefaultBasicConsumer
        {
            public ConsumerFailingOnDelivery(IModel model) : base(model)
            {
            }

            public override void HandleBasicDeliver(string consumerTag,
                ulong deliveryTag,
                bool redelivered,
                string exchange,
                string routingKey,
                IBasicProperties properties,
                ReadOnlyMemory<byte> body)
            {
                throw new Exception("oops");
            }
        }

        private class ConsumerFailingOnCancel : DefaultBasicConsumer
        {
            public ConsumerFailingOnCancel(IModel model) : base(model)
            {
            }

            public override void HandleBasicCancel(string consumerTag)
            {
                throw new Exception("oops");
            }
        }

        private class ConsumerFailingOnShutdown : DefaultBasicConsumer
        {
            public ConsumerFailingOnShutdown(IModel model) : base(model)
            {
            }

            public override void HandleModelShutdown(object model, ShutdownEventArgs reason)
            {
                throw new Exception("oops");
            }
        }

        private class ConsumerFailingOnConsumeOk : DefaultBasicConsumer
        {
            public ConsumerFailingOnConsumeOk(IModel model) : base(model)
            {
            }

            public override void HandleBasicConsumeOk(string consumerTag)
            {
                throw new Exception("oops");
            }
        }

        private class ConsumerFailingOnCancelOk : DefaultBasicConsumer
        {
            public ConsumerFailingOnCancelOk(IModel model) : base(model)
            {
            }

            public override void HandleBasicCancelOk(string consumerTag)
            {
                throw new Exception("oops");
            }
        }

        protected void TestExceptionHandlingWith(IBasicConsumer consumer,
            Action<IModel, string, IBasicConsumer, string> action)
        {
            object o = new object();
            bool notified = false;
            string q = Model.QueueDeclare();

            Model.CallbackException += (m, evt) =>
            {
                notified = true;
                Monitor.PulseAll(o);
            };

            string tag = Model.BasicConsume(q, true, consumer);
            action(Model, q, consumer, tag);
            WaitOn(o);

            Assert.IsTrue(notified);
        }

        [Test]
        public void TestCancelNotificationExceptionHandling()
        {
            IBasicConsumer consumer = new ConsumerFailingOnCancel(Model);
            TestExceptionHandlingWith(consumer, (m, q, c, ct) => m.QueueDelete(q));
        }

        [Test]
        public void TestConsumerCancelOkExceptionHandling()
        {
            IBasicConsumer consumer = new ConsumerFailingOnCancelOk(Model);
            TestExceptionHandlingWith(consumer, (m, q, c, ct) => m.BasicCancel(ct));
        }

        [Test]
        public void TestConsumerConsumeOkExceptionHandling()
        {
            IBasicConsumer consumer = new ConsumerFailingOnConsumeOk(Model);
            TestExceptionHandlingWith(consumer, (m, q, c, ct) => { });
        }

        [Test]
        public void TestConsumerShutdownExceptionHandling()
        {
            IBasicConsumer consumer = new ConsumerFailingOnShutdown(Model);
            TestExceptionHandlingWith(consumer, (m, q, c, ct) => m.Close());
        }

        [Test]
        public void TestDeliveryExceptionHandling()
        {
            IBasicConsumer consumer = new ConsumerFailingOnDelivery(Model);
            TestExceptionHandlingWith(consumer, (m, q, c, ct) => m.BasicPublish("", q, null, encoding.GetBytes("msg")));
        }
    }
}
