using System.Collections.Generic;

namespace RabbitMQ.Client
{
    public interface IEndpointResolver
    {
        /// <summary>
        /// Return all AmqpTcpEndpoints in the order they should be tried.
        /// </summary>
        IEnumerable<AmqpTcpEndpoint> All();
    }
}