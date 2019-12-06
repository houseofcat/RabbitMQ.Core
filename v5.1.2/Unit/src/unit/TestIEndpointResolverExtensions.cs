using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Unit
{
    public class TestEndpointResolver : IEndpointResolver
    {
        private IEnumerable<AmqpTcpEndpoint> endpoints;
        public TestEndpointResolver(IEnumerable<AmqpTcpEndpoint> endpoints)
        {
            this.endpoints = endpoints;
        }

        public IEnumerable<AmqpTcpEndpoint> All()
        {
            return endpoints;
        }
    }

    internal class TestEndpointException : Exception
    {
        public TestEndpointException(string message) : base(message)
        {
        }
    }

    public class TestIEndpointResolverExtensions
    {
        [Test]
        public void SelectOneShouldReturnDefaultWhenThereAreNoEndpoints()
        {
            var ep = new TestEndpointResolver(new List<AmqpTcpEndpoint>());
            Assert.IsNull(ep.SelectOne<AmqpTcpEndpoint>((x) => null));
        }

        [Test]
        public void SelectOneShouldRaiseThrownExceptionWhenThereAreOnlyInaccessibleEndpoints()
        {
            var ep = new TestEndpointResolver(new List<AmqpTcpEndpoint> { new AmqpTcpEndpoint() });
            var thrown = Assert.Throws<AggregateException>(() => ep.SelectOne<AmqpTcpEndpoint>((x) => { throw new TestEndpointException("bananas"); }));
            Assert.That(thrown.InnerExceptions, Has.Exactly(1).TypeOf<TestEndpointException>());
        }

        [Test]
        public void SelectOneShouldReturnFoundEndpoint()
        {
            var ep = new TestEndpointResolver(new List<AmqpTcpEndpoint> { new AmqpTcpEndpoint() });
            Assert.IsNotNull(ep.SelectOne<AmqpTcpEndpoint>((e) => e));
        }
    }
}