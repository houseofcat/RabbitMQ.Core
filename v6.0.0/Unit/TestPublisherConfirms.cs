using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestPublisherConfirms : IntegrationFixture
    {
        private const string QueueName = "RabbitMQ.Client.Unit.TestPublisherConfirms";

        [Test]
        public void TestWaitForConfirmsWithoutTimeout()
        {
            TestWaitForConfirms(200, (ch) =>
            {
                Assert.IsTrue(ch.WaitForConfirms());
            });
        }

        [Test]
        public void TestWaitForConfirmsWithTimeout()
        {
            TestWaitForConfirms(200, (ch) =>
            {
                Assert.IsTrue(ch.WaitForConfirms(TimeSpan.FromSeconds(4)));
            });
        }

        [Test]
        public void TestWaitForConfirmsWithTimeout_AllMessagesAcked_WaitingHasTimedout_ReturnTrue()
        {
            TestWaitForConfirms(200, (ch) =>
            {
                Assert.IsTrue(ch.WaitForConfirms(TimeSpan.FromMilliseconds(1)));
            });
        }

        [Test]
        public void TestWaitForConfirmsWithTimeout_MessageNacked_WaitingHasTimedout_ReturnFalse()
        {
            TestWaitForConfirms(200, (ch) =>
            {
                BasicGetResult message = ch.BasicGet(QueueName, false);

                var fullModel = ch as IFullModel;
                fullModel.HandleBasicNack(message.DeliveryTag, false, false);

                Assert.IsFalse(ch.WaitForConfirms(TimeSpan.FromMilliseconds(1)));
            });
        }

        [Test]
        public void TestWaitForConfirmsWithEvents()
        {
            IModel ch = Conn.CreateModel();
            ch.ConfirmSelect();

            ch.QueueDeclare(QueueName);
            int n = 200;
            // number of event handler invocations
            int c = 0;

            ch.BasicAcks += (_, args) =>
            {
                Interlocked.Increment(ref c);
            };
            try
            {
                for (int i = 0; i < n; i++)
                {
                    ch.BasicPublish("", QueueName, null, encoding.GetBytes("msg"));
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
                ch.WaitForConfirms(TimeSpan.FromSeconds(5));

                // Note: number of event invocations is not guaranteed
                // to be equal to N because acks can be batched,
                // so we primarily care about event handlers being invoked
                // in this test
                Assert.IsTrue(c > 20);
            }
            finally
            {
                ch.QueueDelete(QueueName);
                ch.Close();
            }
        }

        protected void TestWaitForConfirms(int numberOfMessagesToPublish, Action<IModel> fn)
        {
            IModel ch = Conn.CreateModel();
            ch.ConfirmSelect();

            ch.QueueDeclare(QueueName);

            for (int i = 0; i < numberOfMessagesToPublish; i++)
            {
                ch.BasicPublish("", QueueName, null, encoding.GetBytes("msg"));
            }

            try
            {
                fn(ch);
            }
            finally
            {
                ch.QueueDelete(QueueName);
                ch.Close();
            }
        }
    }
}
