using NUnit.Framework;
using RabbitMQ.Client.Events;
using System.Linq;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestConsumerCancelNotify : IntegrationFixture
    {
        protected readonly object lockObject = new object();
        protected bool notifiedCallback;
        protected bool notifiedEvent;
        protected string consumerTag;

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

        [Test]
        public void TestCorrectConsumerTag()
        {
            string q1 = GenerateQueueName();
            string q2 = GenerateQueueName();

            Model.QueueDeclare(q1, false, false, false, null);
            Model.QueueDeclare(q2, false, false, false, null);

            EventingBasicConsumer consumer = new EventingBasicConsumer(Model);
            string consumerTag1 = Model.BasicConsume(q1, true, consumer);
            string consumerTag2 = Model.BasicConsume(q2, true, consumer);

            string notifiedConsumerTag = null;
            consumer.ConsumerCancelled += (sender, args) =>
            {
                lock (lockObject)
                {
                    notifiedConsumerTag = args.ConsumerTags.First();
                    Monitor.PulseAll(lockObject);
                }
            };

            Model.QueueDelete(q1);
            WaitOn(lockObject);
            Assert.AreEqual(consumerTag1, notifiedConsumerTag);

            Model.QueueDelete(q2);
        }

        public void TestConsumerCancel(string queue, bool EventMode, ref bool notified)
        {
            Model.QueueDeclare(queue, false, true, false, null);
            IBasicConsumer consumer = new CancelNotificationConsumer(Model, this, EventMode);
            string actualConsumerTag = Model.BasicConsume(queue, false, consumer);

            Model.QueueDelete(queue);
            WaitOn(lockObject);
            Assert.IsTrue(notified);
            Assert.AreEqual(actualConsumerTag, consumerTag);
        }

        private class CancelNotificationConsumer : DefaultBasicConsumer
        {
            private readonly TestConsumerCancelNotify _testClass;
            private readonly bool _eventMode;

            public CancelNotificationConsumer(IModel model, TestConsumerCancelNotify tc, bool EventMode)
                : base(model)
            {
                _testClass = tc;
                _eventMode = EventMode;
                if (EventMode)
                {
                    ConsumerCancelled += Cancelled;
                }
            }

            public override void HandleBasicCancel(string consumerTag)
            {
                if (!_eventMode)
                {
                    lock (_testClass.lockObject)
                    {
                        _testClass.notifiedCallback = true;
                        _testClass.consumerTag = consumerTag;
                        Monitor.PulseAll(_testClass.lockObject);
                    }
                }
                base.HandleBasicCancel(consumerTag);
            }

            private void Cancelled(object sender, ConsumerEventArgs arg)
            {
                lock (_testClass.lockObject)
                {
                    _testClass.notifiedEvent = true;
                    _testClass.consumerTag = arg.ConsumerTags[0];
                    Monitor.PulseAll(_testClass.lockObject);
                }
            }
        }
    }
}
