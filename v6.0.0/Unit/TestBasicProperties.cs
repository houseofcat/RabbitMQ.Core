using NUnit.Framework;
using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestBasicProperties
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

        [Test]
        public void TestNullableProperties_CanWrite(
            [Values(null, "cluster1")] string clusterId,
            [Values(null, "732E39DC-AF56-46E8-B8A9-079C4B991B2E")] string correlationId,
            [Values(null, "7D221C7E-1788-4D11-9CA5-AC41425047CF")] string messageId
            )
        {
            // Arrange
            var subject = new Framing.BasicProperties
            {
                // Act
                ClusterId = clusterId,
                CorrelationId = correlationId,
                MessageId = messageId
            };

            // Assert
            bool isClusterIdPresent = clusterId != null;
            Assert.AreEqual(isClusterIdPresent, subject.IsClusterIdPresent());

            bool isCorrelationIdPresent = correlationId != null;
            Assert.AreEqual(isCorrelationIdPresent, subject.IsCorrelationIdPresent());

            bool isMessageIdPresent = messageId != null;
            Assert.AreEqual(isMessageIdPresent, subject.IsMessageIdPresent());

            var writer = new Impl.ContentHeaderPropertyWriter(new byte[1024]);
            subject.WritePropertiesTo(ref writer);

            // Read from Stream
            var propertiesFromStream = new Framing.BasicProperties();
            var reader = new Impl.ContentHeaderPropertyReader(writer.Memory.Slice(0, writer.Offset));
            propertiesFromStream.ReadPropertiesFrom(ref reader);

            Assert.AreEqual(clusterId, propertiesFromStream.ClusterId);
            Assert.AreEqual(correlationId, propertiesFromStream.CorrelationId);
            Assert.AreEqual(messageId, propertiesFromStream.MessageId);
            Assert.AreEqual(isClusterIdPresent, propertiesFromStream.IsClusterIdPresent());
            Assert.AreEqual(isCorrelationIdPresent, propertiesFromStream.IsCorrelationIdPresent());
            Assert.AreEqual(isMessageIdPresent, propertiesFromStream.IsMessageIdPresent());
        }
    }
}
