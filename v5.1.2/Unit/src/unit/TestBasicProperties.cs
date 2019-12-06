using NUnit.Framework;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    internal class TestBasicProperties
    {
        [Test]
        public void TestPersistentPropertyChangesDeliveryMode_PersistentTrueDelivery2()
        {
            // Arrange
            var subject = new Framing.BasicProperties
            {
                // Act
                Persistent = true
            };

            // Assert
            Assert.AreEqual(2, subject.DeliveryMode);
            Assert.AreEqual(true, subject.Persistent);
        }

        [Test]
        public void TestPersistentPropertyChangesDeliveryMode_PersistentFalseDelivery1()
        {
            // Arrange
            var subject = new Framing.BasicProperties
            {
                // Act
                Persistent = false
            };

            // Assert
            Assert.AreEqual(1, subject.DeliveryMode);
            Assert.AreEqual(false, subject.Persistent);
        }
    }
}
