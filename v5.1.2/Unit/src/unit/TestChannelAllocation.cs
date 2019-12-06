using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System.Collections.Generic;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestIModelAllocation
    {
        public const int CHANNEL_COUNT = 100;

        private IConnection C;

        public int ModelNumber(IModel model)
        {
            return ((ModelBase)((AutorecoveringModel)model).Delegate).Session.ChannelNumber;
        }

        [SetUp]
        public void Connect()
        {
            C = new ConnectionFactory().CreateConnection();
        }

        [TearDown]
        public void Disconnect()
        {
            C.Close();
        }

        [Test]
        public void AllocateInOrder()
        {
            for (int i = 1; i <= CHANNEL_COUNT; i++)
                Assert.AreEqual(i, ModelNumber(C.CreateModel()));
        }

        [Test]
        public void AllocateAfterFreeingLast()
        {
            IModel ch = C.CreateModel();
            Assert.AreEqual(1, ModelNumber(ch));
            ch.Close();
            ch = C.CreateModel();
            Assert.AreEqual(1, ModelNumber(ch));
        }

        public int CompareModels(IModel x, IModel y)
        {
            int i = ModelNumber(x);
            int j = ModelNumber(y);
            return (i < j) ? -1 : (i == j) ? 0 : 1;
        }

        [Test]
        public void AllocateAfterFreeingMany()
        {
            List<IModel> channels = new List<IModel>();

            for (int i = 1; i <= CHANNEL_COUNT; i++)
                channels.Add(C.CreateModel());

            foreach (IModel channel in channels)
            {
                channel.Close();
            }

            channels = new List<IModel>();

            for (int j = 1; j <= CHANNEL_COUNT; j++)
                channels.Add(C.CreateModel());

            // In the current implementation the list should actually
            // already be sorted, but we don't want to force that behaviour
            channels.Sort(CompareModels);

            int k = 1;
            foreach (IModel channel in channels)
                Assert.AreEqual(k++, ModelNumber(channel));
        }
    }
}
