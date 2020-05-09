using NUnit.Framework;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    internal class TestConnectionFactoryContinuationTimeout : IntegrationFixture
    {
        [Test]
        public void TestConnectionFactoryContinuationTimeoutOnRecoveringConnection()
        {
            var continuationTimeout = TimeSpan.FromSeconds(777);
            using (IConnection c = CreateConnectionWithContinuationTimeout(true, continuationTimeout))
            {
                Assert.AreEqual(continuationTimeout, c.CreateModel().ContinuationTimeout);
            }
        }

        [Test]
        public void TestConnectionFactoryContinuationTimeoutOnNonRecoveringConnection()
        {
            var continuationTimeout = TimeSpan.FromSeconds(777);
            using (IConnection c = CreateConnectionWithContinuationTimeout(false, continuationTimeout))
            {
                Assert.AreEqual(continuationTimeout, c.CreateModel().ContinuationTimeout);
            }
        }
    }
}
