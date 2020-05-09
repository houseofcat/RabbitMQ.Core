using NUnit.Framework;

using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestBasicGet : IntegrationFixture
    {
        [Test]
        public void TestBasicGetWithClosedChannel()
        {
            WithNonEmptyQueue((_, q) =>
               {
                   WithClosedModel(cm =>
                   {
                       Assert.Throws(Is.InstanceOf<AlreadyClosedException>(), () => cm.BasicGet(q, true));
                   });
               });
        }

        [Test]
        public void TestBasicGetWithEmptyResponse()
        {
            WithEmptyQueue((model, queue) =>
            {
                BasicGetResult res = model.BasicGet(queue, false);
                Assert.IsNull(res);
            });
        }

        [Test]
        public void TestBasicGetWithNonEmptyResponseAndAutoAckMode()
        {
            const string msg = "for basic.get";
            WithNonEmptyQueue((model, queue) =>
            {
                BasicGetResult res = model.BasicGet(queue, true);
                Assert.AreEqual(msg, encoding.GetString(res.Body.ToArray()));
                AssertMessageCount(queue, 0);
            }, msg);
        }
    }
}
