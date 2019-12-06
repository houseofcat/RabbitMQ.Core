using NUnit.Framework;
using RabbitMQ.Client.Events;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestConsumerCancelNotify : IntegrationFixture
    {
        protected readonly object lockObject = new object();
        protected bool notifiedCallback;
        protected bool notifiedEvent;

        [Test]
        public void TestConsumerCancelNotification()
        {
            TestConsumerCancel("queue_consumer_cancel_notify", false, ref notifiedCallback);
        }

        [Test]
        public void TestConsumerCancelEvent()
        {
            TestConsumerCancel("queue_consumer_cancel_event", true, ref notifiedEvent);
        }

        public void TestConsumerCancel(string queue, bool EventMode, ref bool notified)
        {
            Model.QueueDeclare(queue, false, true, false, null);
            IBasicConsumer consumer = new CancelNotificationConsumer(Model, this, EventMode);
            Model.BasicConsume(queue, false, consumer);

            Model.QueueDelete(queue);
            WaitOn(lockObject);
            Assert.IsTrue(notified);
        }

        private class CancelNotificationConsumer : DefaultBasicConsumer
        {
            private TestConsumerCancelNotify testClass;
            private bool EventMode;

            public CancelNotificationConsumer(IModel model, TestConsumerCancelNotify tc, bool EventMode)
                : base(model)
            {
                this.testClass = tc;
                this.EventMode = EventMode;
                if (EventMode)
                {
                    ConsumerCancelled += Cancelled;
                }
            }

            public override void HandleBasicCancel(string consumerTag)
            {
                if (!EventMode)
                {
                    lock (testClass.lockObject)
                    {
                        testClass.notifiedCallback = true;
                        Monitor.PulseAll(testClass.lockObject);
                    }
                }
                base.HandleBasicCancel(consumerTag);
            }

            private void Cancelled(object sender, ConsumerEventArgs arg)
            {
                lock (testClass.lockObject)
                {
                    testClass.notifiedEvent = true;
                    Monitor.PulseAll(testClass.lockObject);
                }
            }
        }
    }
}
