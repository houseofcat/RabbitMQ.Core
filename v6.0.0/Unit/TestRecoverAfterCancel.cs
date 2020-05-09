using NUnit.Framework;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Impl;
using RabbitMQ.Util;
using System;
using System.Text;

#pragma warning disable 0618

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestRecoverAfterCancel
    {
        IConnection Connection;
        IModel Channel;
        string Queue;
        int callbackCount;

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
        public void TestRecoverAfterCancel_()
        {
            UTF8Encoding enc = new UTF8Encoding();
            Channel.BasicPublish("", Queue, null, enc.GetBytes("message"));
            EventingBasicConsumer Consumer = new EventingBasicConsumer(Channel);
            SharedQueue<BasicDeliverEventArgs> EventQueue = new SharedQueue<BasicDeliverEventArgs>();
            Consumer.Received += (_, e) => EventQueue.Enqueue(e);

            string CTag = Channel.BasicConsume(Queue, false, Consumer);
            BasicDeliverEventArgs Event = EventQueue.Dequeue();
            Channel.BasicCancel(CTag);
            Channel.BasicRecover(true);

            EventingBasicConsumer Consumer2 = new EventingBasicConsumer(Channel);
            SharedQueue<BasicDeliverEventArgs> EventQueue2 = new SharedQueue<BasicDeliverEventArgs>();
            Consumer2.Received += (_, e) => EventQueue2.Enqueue(e);
            Channel.BasicConsume(Queue, false, Consumer2);
            BasicDeliverEventArgs Event2 = EventQueue2.Dequeue();

            CollectionAssert.AreEqual(Event.Body.ToArray(), Event2.Body.ToArray());
            Assert.IsFalse(Event.Redelivered);
            Assert.IsTrue(Event2.Redelivered);
        }

        [Test]
        public void TestRecoverCallback()
        {
            callbackCount = 0;
            Channel.BasicRecoverOk += IncrCallback;
            Channel.BasicRecover(true);
            Assert.AreEqual(1, callbackCount);
        }

        void IncrCallback(object sender, EventArgs args)
        {
            callbackCount++;
        }
    }
}
