using NUnit.Framework;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestExtensions : IntegrationFixture
    {
        [Test]
        public void TestConfirmBarrier()
        {
            Model.ConfirmSelect();
            for (int i = 0; i < 10; i++)
            {
                Model.BasicPublish("", string.Empty, null, new byte[] { });
            }
            Assert.That(Model.WaitForConfirms(), Is.True);
        }

        [Test]
        public void TestConfirmBeforeWait()
        {
            Assert.Throws(typeof(InvalidOperationException), () => Model.WaitForConfirms());
        }

        [Test]
        public void TestExchangeBinding()
        {
            Model.ConfirmSelect();

            Model.ExchangeDeclare("src", ExchangeType.Direct, false, false, null);
            Model.ExchangeDeclare("dest", ExchangeType.Direct, false, false, null);
            string queue = Model.QueueDeclare();

            Model.ExchangeBind("dest", "src", string.Empty);
            Model.ExchangeBind("dest", "src", string.Empty);
            Model.QueueBind(queue, "dest", string.Empty);

            Model.BasicPublish("src", string.Empty, null, new byte[] { });
            Model.WaitForConfirms();
            Assert.IsNotNull(Model.BasicGet(queue, true));

            Model.ExchangeUnbind("dest", "src", string.Empty);
            Model.BasicPublish("src", string.Empty, null, new byte[] { });
            Model.WaitForConfirms();
            Assert.IsNull(Model.BasicGet(queue, true));

            Model.ExchangeDelete("src");
            Model.ExchangeDelete("dest");
        }
    }
}
