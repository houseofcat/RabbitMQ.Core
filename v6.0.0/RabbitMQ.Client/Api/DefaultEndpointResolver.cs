using System;
using System.Collections.Generic;
using System.Linq;

namespace RabbitMQ.Client
{
    public class DefaultEndpointResolver : IEndpointResolver
    {
        private readonly List<AmqpTcpEndpoint> _endpoints;
        private readonly Random _rnd = new Random();

        public DefaultEndpointResolver(IEnumerable<AmqpTcpEndpoint> tcpEndpoints)
        {
            _endpoints = tcpEndpoints.ToList();
        }

        public IEnumerable<AmqpTcpEndpoint> All()
        {
            return _endpoints.OrderBy(_ => _rnd.Next());
        }
    }
}
