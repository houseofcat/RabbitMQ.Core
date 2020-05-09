using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    internal class TestModelShutdown : IntegrationFixture
    {
        [Test]
        public void TestConsumerDispatcherShutdown()
        {
            var m = (AutorecoveringModel)Model;
            var latch = new ManualResetEvent(false);

            Model.ModelShutdown += (model, args) =>
            {
                latch.Set();
            };
            Assert.IsFalse(m.ConsumerDispatcher.IsShutdown, "dispatcher should NOT be shut down before Close");
            Model.Close();
            Wait(latch, TimeSpan.FromSeconds(3));
            Assert.IsTrue(m.ConsumerDispatcher.IsShutdown, "dispatcher should be shut down after Close");
        }
    }
}
