using NUnit.Framework;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestConcurrentAccessWithSharedConnection : IntegrationFixture
    {
        internal const int Threads = 32;
        internal CountdownEvent _latch;
        internal TimeSpan _completionTimeout = TimeSpan.FromSeconds(90);

        [SetUp]
        public override void Init()
        {
            base.Init();
            ThreadPool.SetMinThreads(Threads, Threads);
            _latch = new CountdownEvent(Threads);
        }

        [TearDown]
        protected override void ReleaseResources()
        {
            _latch.Dispose();
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingWithBlankMessages()
        {
            TestConcurrentChannelOpenAndPublishingWithBody(Encoding.ASCII.GetBytes(string.Empty), 30);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase1()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(64);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase2()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(256);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase3()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(1024);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase4()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(8192);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase5()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(32768, 20);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase6()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(100000, 15);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase7()
        {
            // surpasses default frame size
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(131072, 12);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase8()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(270000, 10);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase9()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(540000, 6);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase10()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(1000000, 2);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase11()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(1500000, 1);
        }

        [Test]
        public void TestConcurrentChannelOpenAndPublishingCase12()
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(128000000, 1);
        }

        [Test]
        public void TestConcurrentChannelOpenCloseLoop()
        {
            TestConcurrentChannelOperations((conn) =>
            {
                IModel ch = conn.CreateModel();
                ch.Close();
            }, 50);
        }

        internal void TestConcurrentChannelOpenAndPublishingWithBodyOfSize(int length)
        {
            TestConcurrentChannelOpenAndPublishingWithBodyOfSize(length, 30);
        }

        internal void TestConcurrentChannelOpenAndPublishingWithBodyOfSize(int length, int iterations)
        {
            string s = "payload";
            if (length > s.Length)
            {
                s.PadRight(length);
            }

            TestConcurrentChannelOpenAndPublishingWithBody(Encoding.ASCII.GetBytes(s), iterations);
        }

        internal void TestConcurrentChannelOpenAndPublishingWithBody(byte[] body, int iterations)
        {
            TestConcurrentChannelOperations((conn) =>
            {
                // publishing on a shared channel is not supported
                // and would missing the point of this test anyway
                IModel ch = Conn.CreateModel();
                ch.ConfirmSelect();
                foreach (int j in Enumerable.Range(0, 200))
                {
                    ch.BasicPublish(exchange: "", routingKey: "_______", basicProperties: ch.CreateBasicProperties(), body: body);
                }
                ch.WaitForConfirms(TimeSpan.FromSeconds(40));
            }, iterations);
        }

        internal void TestConcurrentChannelOperations(Action<IConnection> actions,
            int iterations)
        {
            TestConcurrentChannelOperations(actions, iterations, _completionTimeout);
        }

        internal void TestConcurrentChannelOperations(Action<IConnection> actions,
            int iterations, TimeSpan timeout)
        {
            Task[] tasks = Enumerable.Range(0, Threads).Select(_ =>
            {
                return Task.Run(() =>
                {
                    foreach (int j in Enumerable.Range(0, iterations))
                    {
                        actions(Conn);
                    }

                    _latch.Signal();
                });
            }).ToArray();

            Assert.IsTrue(_latch.Wait(timeout));
            // incorrect frame interleaving in these tests will result
            // in an unrecoverable connection-level exception, thus
            // closing the connection
            Assert.IsTrue(Conn.IsOpen);
        }
    }
}
