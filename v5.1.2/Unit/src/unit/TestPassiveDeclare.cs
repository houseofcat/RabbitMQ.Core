using NUnit.Framework;
using RabbitMQ.Client.Exceptions;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestPassiveDeclare : IntegrationFixture
    {
        [Test]
        public void TestPassiveExchangeDeclareWhenExchangeDoesNotExist()
        {
            Assert.Throws(Is.InstanceOf<OperationInterruptedException>(),
                () => Model.ExchangeDeclarePassive(Guid.NewGuid().ToString()));
        }

        [Test]
        public void TestPassiveQueueDeclareWhenQueueDoesNotExist()
        {
            Assert.Throws(Is.InstanceOf<OperationInterruptedException>(),
                () => Model.QueueDeclarePassive(Guid.NewGuid().ToString()));
        }
    }
}
