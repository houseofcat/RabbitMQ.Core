using NUnit.Framework;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestConfirmSelect : IntegrationFixture
    {
        [Test]
        public void TestConfirmSelectIdempotency()
        {
            Model.ConfirmSelect();
            Assert.AreEqual(1, Model.NextPublishSeqNo);
            Publish();
            Assert.AreEqual(2, Model.NextPublishSeqNo);
            Publish();
            Assert.AreEqual(3, Model.NextPublishSeqNo);

            Model.ConfirmSelect();
            Publish();
            Assert.AreEqual(4, Model.NextPublishSeqNo);
            Publish();
            Assert.AreEqual(5, Model.NextPublishSeqNo);
            Publish();
            Assert.AreEqual(6, Model.NextPublishSeqNo);
        }

        protected void Publish()
        {
            Model.BasicPublish("", "amq.fanout", null, encoding.GetBytes("message"));
        }
    }
}
