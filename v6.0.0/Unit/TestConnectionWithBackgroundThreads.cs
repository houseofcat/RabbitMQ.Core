using NUnit.Framework;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestConnectionWithBackgroundThreads
    {
        [Test]
        public void TestWithBackgroundThreadsEnabled()
        {
            ConnectionFactory connFactory = new ConnectionFactory
            {
                UseBackgroundThreadsForIO = true
            };

            IConnection conn = connFactory.CreateConnection();
            IModel ch = conn.CreateModel();

            // sanity check
            string q = ch.QueueDeclare();
            ch.QueueDelete(q);

            ch.Close();
            conn.Close();
        }
    }
}

