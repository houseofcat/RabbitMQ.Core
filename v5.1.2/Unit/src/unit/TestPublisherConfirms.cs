using NUnit.Framework;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestPublisherConfirms : IntegrationFixture
    {
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
        public void TestWaitForConfirmsWithEvents()
        {
            var ch = Conn.CreateModel();
            ch.ConfirmSelect();

            var q = ch.QueueDeclare().QueueName;
            var n = 200;
            // number of event handler invocations
            var c = 0;

            ch.BasicAcks += (_, args) =>
            {
                Interlocked.Increment(ref c);
            };
            try
            {
                for (int i = 0; i < n; i++)
                {
                    ch.BasicPublish("", q, null, encoding.GetBytes("msg"));
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
                ch.QueueDelete(q);
                ch.Close();
            }
        }

        protected void TestWaitForConfirms(int numberOfMessagesToPublish, Action<IModel> fn)
        {
            var ch = Conn.CreateModel();
            ch.ConfirmSelect();

            var q = ch.QueueDeclare().QueueName;

            for (int i = 0; i < numberOfMessagesToPublish; i++)
            {
                ch.BasicPublish("", q, null, encoding.GetBytes("msg"));
            }
            try
            {
                fn(ch);
            }
            finally
            {
                ch.QueueDelete(q);
                ch.Close();
            }
        }
    }
}
