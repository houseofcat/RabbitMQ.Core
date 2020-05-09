using NUnit.Framework;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestPublicationAddress
    {
        [Test]
        public void TestParseOk()
        {
            string uriLike = "fanout://name/key";
            PublicationAddress addr = PublicationAddress.Parse(uriLike);
            Assert.AreEqual(ExchangeType.Fanout, addr.ExchangeType);
            Assert.AreEqual("name", addr.ExchangeName);
            Assert.AreEqual("key", addr.RoutingKey);
            Assert.AreEqual(uriLike, addr.ToString());
        }

        [Test]
        public void TestParseFail()
        {
            Assert.IsNull(PublicationAddress.Parse("not a valid uri"));
        }

        [Test]
        public void TestEmptyExchangeName()
        {
            string uriLike = "direct:///key";
            PublicationAddress addr = PublicationAddress.Parse(uriLike);
            Assert.AreEqual(ExchangeType.Direct, addr.ExchangeType);
            Assert.AreEqual("", addr.ExchangeName);
            Assert.AreEqual("key", addr.RoutingKey);
            Assert.AreEqual(uriLike, addr.ToString());
        }

        [Test]
        public void TestEmptyRoutingKey()
        {
            string uriLike = "direct://exch/";
            PublicationAddress addr = PublicationAddress.Parse(uriLike);
            Assert.AreEqual(ExchangeType.Direct, addr.ExchangeType);
            Assert.AreEqual("exch", addr.ExchangeName);
            Assert.AreEqual("", addr.RoutingKey);
            Assert.AreEqual(uriLike, addr.ToString());
        }

        [Test]
        public void TestExchangeTypeValidation()
        {
            string uriLike = "direct:///";
            PublicationAddress addr = PublicationAddress.Parse(uriLike);
            int found = 0;
            foreach (string exchangeType in ExchangeType.All())
            {
                if (exchangeType.Equals(addr.ExchangeType))
                {
                    found++;
                }
            }
            Assert.AreEqual(1, found);
        }

        [Test]
        public void TestMissingExchangeType()
        {
            Assert.IsNull(PublicationAddress.Parse("://exch/key"));
        }
    }
}
