using NUnit.Framework;


namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestFloodPublishing : IntegrationFixture
    {
        [SetUp]
        public override void Init()
        {
            var connFactory = new ConnectionFactory()
            {
                RequestedHeartbeat = 60,
                AutomaticRecoveryEnabled = false
            };
            Conn = connFactory.CreateConnection();
            Model = Conn.CreateModel();
        }

        [Test, Category("LongRunning")]
        public void TestUnthrottledFloodPublishing()
        {
            Conn.ConnectionShutdown += (_, args) =>
            {
                if (args.Initiator != ShutdownInitiator.Application)
                {
                    Assert.Fail("Unexpected connection shutdown!");
                }
            };

            bool elapsed = false;
            var t = new System.Threading.Timer((_obj) => { elapsed = true; }, null, 1000 * 185, -1);

            while (!elapsed)
            {
                Model.BasicPublish("", "", null, new byte[2048]);
            }
            Assert.IsTrue(Conn.IsOpen);
            t.Dispose();
        }
    }
}
