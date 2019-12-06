using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestConnectionShutdown : IntegrationFixture
    {
        [Test]
        public void TestShutdownSignalPropagationToChannels()
        {
            var latch = new ManualResetEvent(false);

            this.Model.ModelShutdown += (model, args) =>
            {
                latch.Set();
            };
            Conn.Close();

            Wait(latch, TimeSpan.FromSeconds(3));
        }

        [Test]
        public void TestConsumerDispatcherShutdown()
        {
            var m = (AutorecoveringModel)Model;
            var latch = new ManualResetEvent(false);

            this.Model.ModelShutdown += (model, args) =>
            {
                latch.Set();
            };
            Assert.IsFalse(m.ConsumerDispatcher.IsShutdown, "dispatcher should NOT be shut down before Close");
            Conn.Close();
            Wait(latch, TimeSpan.FromSeconds(3));
            Assert.IsTrue(m.ConsumerDispatcher.IsShutdown, "dispatcher should be shut down after Close");
        }
    }
}
