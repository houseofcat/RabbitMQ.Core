using NUnit.Framework;
using RabbitMQ.Client.Events;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestEventingConsumer : IntegrationFixture
    {
        [Test]
        public void TestEventingConsumerRegistrationEvents()
        {
            string q = Model.QueueDeclare();

            var registeredLatch = new ManualResetEvent(false);
            object registeredSender = null;
            var unregisteredLatch = new ManualResetEvent(false);
            object unregisteredSender = null;

            EventingBasicConsumer ec = new EventingBasicConsumer(Model);
            ec.Registered += (s, args) =>
            {
                registeredSender = s;
                registeredLatch.Set();
            };

            ec.Unregistered += (s, args) =>
            {
                unregisteredSender = s;
                unregisteredLatch.Set();
            };

            string tag = Model.BasicConsume(q, false, ec);
            Wait(registeredLatch);

            Assert.IsNotNull(registeredSender);
            Assert.AreEqual(ec, registeredSender);
            Assert.AreEqual(Model, ((EventingBasicConsumer)registeredSender).Model);

            Model.BasicCancel(tag);
            Wait(unregisteredLatch);
            Assert.IsNotNull(unregisteredSender);
            Assert.AreEqual(ec, unregisteredSender);
            Assert.AreEqual(Model, ((EventingBasicConsumer)unregisteredSender).Model);
        }

        [Test]
        public void TestEventingConsumerDeliveryEvents()
        {
            string q = Model.QueueDeclare();
            object o = new object();

            bool receivedInvoked = false;
            object receivedSender = null;

            EventingBasicConsumer ec = new EventingBasicConsumer(Model);
            ec.Received += (s, args) =>
            {
                receivedInvoked = true;
                receivedSender = s;

                Monitor.PulseAll(o);
            };

            Model.BasicConsume(q, true, ec);
            Model.BasicPublish("", q, null, encoding.GetBytes("msg"));

            WaitOn(o);
            Assert.IsTrue(receivedInvoked);
            Assert.IsNotNull(receivedSender);
            Assert.AreEqual(ec, receivedSender);
            Assert.AreEqual(Model, ((EventingBasicConsumer)receivedSender).Model);

            bool shutdownInvoked = false;
            object shutdownSender = null;

            ec.Shutdown += (s, args) =>
            {
                shutdownInvoked = true;
                shutdownSender = s;

                Monitor.PulseAll(o);
            };

            Model.Close();
            WaitOn(o);

            Assert.IsTrue(shutdownInvoked);
            Assert.IsNotNull(shutdownSender);
            Assert.AreEqual(ec, shutdownSender);
            Assert.AreEqual(Model, ((EventingBasicConsumer)shutdownSender).Model);
        }
    }
}
