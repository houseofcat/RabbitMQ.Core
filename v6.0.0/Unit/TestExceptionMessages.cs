using NUnit.Framework;
using RabbitMQ.Client.Exceptions;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestExceptionMessages : IntegrationFixture
    {
        [Test]
        public void TestAlreadyClosedExceptionMessage()
        {
            string uuid = System.Guid.NewGuid().ToString();
            try
            {
                Model.QueueDeclarePassive(uuid);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.TypeOf(typeof(OperationInterruptedException)));
            }

            Assert.IsFalse(Model.IsOpen);

            try
            {
                Model.QueueDeclarePassive(uuid);
            }
            catch (AlreadyClosedException e)
            {
                Assert.That(e, Is.TypeOf(typeof(AlreadyClosedException)));
                Assert.IsTrue(e.Message.StartsWith("Already closed"));
            }
        }
    }
}
