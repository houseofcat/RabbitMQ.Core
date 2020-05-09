using NUnit.Framework;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestFloodPublishing
    {
        private readonly byte[] _body = new byte[2048];
        private readonly TimeSpan _tenSeconds = TimeSpan.FromSeconds(10);

        [Test]
        public void TestUnthrottledFloodPublishing()
        {
            var connFactory = new ConnectionFactory()
            {
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                AutomaticRecoveryEnabled = false
            };

            using (var conn = connFactory.CreateConnection())
            {
                using (var model = conn.CreateModel())
                {
                    conn.ConnectionShutdown += (_, args) =>
                    {
                        if (args.Initiator != ShutdownInitiator.Application)
                        {
                            Assert.Fail("Unexpected connection shutdown!");
                        }
                    };

                    bool shouldStop = false;
                    DateTime now = DateTime.Now;
                    DateTime stopTime = DateTime.Now.Add(_tenSeconds);
                    for (int i = 0; i < 65535 * 64; i++)
                    {
                        if (i % 65536 == 0)
                        {
                            now = DateTime.Now;
                            shouldStop = DateTime.Now > stopTime;
                            if (shouldStop)
                            {
                                break;
                            }
                        }
                        model.BasicPublish("", "", null, _body);
                    }
                    Assert.IsTrue(conn.IsOpen);
                }
            }
        }

        [Test]
        public void TestMultithreadFloodPublishing()
        {
            string testName = TestContext.CurrentContext.Test.FullName;
            string message = string.Format("Hello from test {0}", testName);
            byte[] sendBody = Encoding.UTF8.GetBytes(message);
            int publishCount = 4096;
            int receivedCount = 0;
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);

            var cf = new ConnectionFactory()
            {
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                AutomaticRecoveryEnabled = false
            };

            using (IConnection c = cf.CreateConnection())
            {
                string queueName = null;
                using (IModel m = c.CreateModel())
                {
                    QueueDeclareOk q = m.QueueDeclare();
                    queueName = q.QueueName;
                }

                Task pub = Task.Run(() =>
                {
                    using (IModel m = c.CreateModel())
                    {
                        IBasicProperties bp = m.CreateBasicProperties();
                        for (int i = 0; i < publishCount; i++)
                        {
                            m.BasicPublish(string.Empty, queueName, bp, sendBody);
                        }
                    }
                });

                using (IModel consumerModel = c.CreateModel())
                {
                    var consumer = new EventingBasicConsumer(consumerModel);
                    consumer.Received += (o, a) =>
                    {
                        string receivedMessage = Encoding.UTF8.GetString(a.Body.ToArray());
                        Assert.AreEqual(message, receivedMessage);
                        Interlocked.Increment(ref receivedCount);
                        if (receivedCount == publishCount)
                        {
                            autoResetEvent.Set();
                        }
                    };
                    consumerModel.BasicConsume(queueName, true, consumer);
                    Assert.IsTrue(pub.Wait(_tenSeconds));
                    Assert.IsTrue(autoResetEvent.WaitOne(_tenSeconds));
                }

                Assert.AreEqual(publishCount, receivedCount);
            }
        }
    }
}
