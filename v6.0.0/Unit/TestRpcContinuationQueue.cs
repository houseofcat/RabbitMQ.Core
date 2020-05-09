using NUnit.Framework;
using RabbitMQ.Client.Impl;
using System;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestRpcContinuationQueue
    {
        [Test]
        public void TestRpcContinuationQueueEnqueueAndRelease()
        {
            RpcContinuationQueue queue = new RpcContinuationQueue();
            var inputContinuation = new SimpleBlockingRpcContinuation();
            queue.Enqueue(inputContinuation);
            IRpcContinuation outputContinuation = queue.Next();
            Assert.AreEqual(outputContinuation, inputContinuation);
        }

        [Test]
        public void TestRpcContinuationQueueEnqueueAndRelease2()
        {
            RpcContinuationQueue queue = new RpcContinuationQueue();
            var inputContinuation = new SimpleBlockingRpcContinuation();
            queue.Enqueue(inputContinuation);
            IRpcContinuation outputContinuation = queue.Next();
            Assert.AreEqual(outputContinuation, inputContinuation);
            IRpcContinuation outputContinuation1 = queue.Next();
            Assert.AreNotEqual(outputContinuation1, inputContinuation);
        }

        [Test]
        public void TestRpcContinuationQueueEnqueue2()
        {
            RpcContinuationQueue queue = new RpcContinuationQueue();
            var inputContinuation = new SimpleBlockingRpcContinuation();
            var inputContinuation1 = new SimpleBlockingRpcContinuation();
            queue.Enqueue(inputContinuation);
            Assert.Throws(typeof(NotSupportedException), () =>
            {
                queue.Enqueue(inputContinuation1);
            });
        }
    }
}
