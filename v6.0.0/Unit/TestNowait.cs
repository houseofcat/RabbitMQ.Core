using NUnit.Framework;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestNoWait : IntegrationFixture
    {
        [Test]
        public void TestQueueDeclareNoWait()
        {
            string q = GenerateQueueName();
            Model.QueueDeclareNoWait(q, false, true, false, null);
            Model.QueueDeclarePassive(q);
        }

        [Test]
        public void TestQueueBindNoWait()
        {
            string q = GenerateQueueName();
            Model.QueueDeclareNoWait(q, false, true, false, null);
            Model.QueueBindNoWait(q, "amq.fanout", "", null);
        }

        [Test]
        public void TestQueueDeleteNoWait()
        {
            string q = GenerateQueueName();
            Model.QueueDeclareNoWait(q, false, true, false, null);
            Model.QueueDeleteNoWait(q, false, false);
        }

        [Test]
        public void TestExchangeDeclareNoWait()
        {
            string x = GenerateExchangeName();
            try
            {
                Model.ExchangeDeclareNoWait(x, "fanout", false, true, null);
                Model.ExchangeDeclarePassive(x);
            }
            finally
            {
                Model.ExchangeDelete(x);
            }
        }

        [Test]
        public void TestExchangeBindNoWait()
        {
            string x = GenerateExchangeName();
            try
            {
                Model.ExchangeDeclareNoWait(x, "fanout", false, true, null);
                Model.ExchangeBindNoWait(x, "amq.fanout", "", null);
            }
            finally
            {
                Model.ExchangeDelete(x);
            }
        }

        [Test]
        public void TestExchangeUnbindNoWait()
        {
            string x = GenerateExchangeName();
            try
            {
                Model.ExchangeDeclare(x, "fanout", false, true, null);
                Model.ExchangeBind(x, "amq.fanout", "", null);
                Model.ExchangeUnbindNoWait(x, "amq.fanout", "", null);
            }
            finally
            {
                Model.ExchangeDelete(x);
            }
        }

        [Test]
        public void TestExchangeDeleteNoWait()
        {
            string x = GenerateExchangeName();
            Model.ExchangeDeclareNoWait(x, "fanout", false, true, null);
            Model.ExchangeDeleteNoWait(x, false);
        }
    }
}
