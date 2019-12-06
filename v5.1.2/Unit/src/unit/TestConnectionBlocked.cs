using NUnit.Framework;
using RabbitMQ.Client.Events;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestConnectionBlocked : IntegrationFixture
    {
        private readonly object _lockObject = new object();
        private bool _notified;

        public void HandleBlocked(object sender, ConnectionBlockedEventArgs args)
        {
            Unblock();
        }

        public void HandleUnblocked(object sender, EventArgs ea)
        {
            lock (_lockObject)
            {
                _notified = true;
                Monitor.PulseAll(_lockObject);
            }
        }

        protected override void ReleaseResources()
        {
            Unblock();
        }

        [Test]
        public void TestConnectionBlockedNotification()
        {
            Conn.ConnectionBlocked += HandleBlocked;
            Conn.ConnectionUnblocked += HandleUnblocked;

            Block();
            lock (_lockObject)
            {
                if (!_notified)
                {
                    Monitor.Wait(_lockObject, TimeSpan.FromSeconds(15));
                }
            }
            if (!_notified)
            {
                Unblock();
                Assert.Fail("Unblock notification not received.");
            }
        }
    }
}
