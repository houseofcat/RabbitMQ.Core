using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestRecoverAfterCancel
    {
        private IConnection Connection;
        private IModel Channel;
        private string Queue;
        private int callbackCount;

        public int ModelNumber(IModel model)
        {
            return ((ModelBase)model).Session.ChannelNumber;
        }

        [SetUp]
        public void Connect()
        {
            Connection = new ConnectionFactory().CreateConnection();
            Channel = Connection.CreateModel();
            Queue = Channel.QueueDeclare("", false, true, false, null);
        }

        [TearDown]
        public void Disconnect()
        {
            Connection.Abort();
        }

        [Test]
        public void TestRecoverCallback()
        {
            callbackCount = 0;
            Channel.BasicRecoverOk += IncrCallback;
            Channel.BasicRecover(true);
            Assert.AreEqual(1, callbackCount);
        }

        private void IncrCallback(object sender, EventArgs args)
        {
            callbackCount++;
        }
    }
}
