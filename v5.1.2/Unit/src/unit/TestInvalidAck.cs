using NUnit.Framework;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestInvalidAck : IntegrationFixture
    {
        [Test]
        public void TestAckWithUnknownConsumerTagAndMultipleFalse()
        {
            object o = new object();
            bool shutdownFired = false;
            ShutdownEventArgs shutdownArgs = null;
            Model.ModelShutdown += (s, args) =>
            {
                shutdownFired = true;
                shutdownArgs = args;
                Monitor.PulseAll(o);
            };

            Model.BasicAck(123456, false);
            WaitOn(o);
            Assert.IsTrue(shutdownFired);
            AssertPreconditionFailed(shutdownArgs);
        }
    }
}
