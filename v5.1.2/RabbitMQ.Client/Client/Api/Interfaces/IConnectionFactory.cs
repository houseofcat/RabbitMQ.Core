using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public interface IConnectionFactory
    {
        /// <summary>
        /// Dictionary of client properties to be sent to the server.
        /// </summary>
        IDictionary<string, object> ClientProperties { get; set; }

        /// <summary>
        /// Password to use when authenticating to the server.
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Maximum channel number to ask for.
        /// </summary>
        ushort RequestedChannelMax { get; set; }

        /// <summary>
        /// Frame-max parameter to ask for (in bytes).
        /// </summary>
        uint RequestedFrameMax { get; set; }

        /// <summary>
        /// Heartbeat setting to request (in seconds).
        /// </summary>
        ushort RequestedHeartbeat { get; set; }

        /// <summary>
        /// When set to true, background threads will be used for I/O and heartbeats.
        /// </summary>
        bool UseBackgroundThreadsForIO { get; set; }

        /// <summary>
        /// Username to use when authenticating to the server.
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Virtual host to access during this connection.
        /// </summary>
        string VirtualHost { get; set; }

        /// <summary>
        /// Sets or gets the AMQP Uri to be used for connections.
        /// </summary>
        Uri Uri { get; set; }

        /// <summary>
        /// Given a list of mechanism names supported by the server, select a preferred mechanism,
        /// or null if we have none in common.
        /// </summary>
        IAuthMechanismFactory AuthMechanismFactory(IList<string> mechanismNames);

        /// <summary>
        /// Create a connection to the specified endpoint.
        /// </summary>
        IConnection CreateConnection();

        /// <summary>
        /// Create a connection to the specified endpoint.
        /// </summary>
        /// <param name="clientProvidedName">
        /// Application-specific connection name, will be displayed in the management UI
        /// if RabbitMQ server supports it. This value doesn't have to be unique and cannot
        /// be used as a connection identifier, e.g. in HTTP API requests.
        /// This value is supposed to be human-readable.
        /// </param>
        /// <returns></returns>
        IConnection CreateConnection(string clientProvidedName);

        /// <summary>
        /// Connects to the first reachable hostname from the list.
        /// </summary>
        /// <param name="hostnames">List of host names to use</param>
        /// <returns></returns>
        IConnection CreateConnection(IList<string> hostnames);

        /// <summary>
        /// Connects to the first reachable hostname from the list.
        /// </summary>
        /// <param name="hostnames">List of host names to use</param>
        /// <param name="clientProvidedName">
        /// Application-specific connection name, will be displayed in the management UI
        /// if RabbitMQ server supports it. This value doesn't have to be unique and cannot
        /// be used as a connection identifier, e.g. in HTTP API requests.
        /// This value is supposed to be human-readable.
        /// </param>
        /// <returns></returns>
        IConnection CreateConnection(IList<string> hostnames, string clientProvidedName);

        /// <summary>
        /// Create a connection using a list of endpoints.
        /// The selection behaviour can be overriden by configuring the EndpointResolverFactory.
        /// </summary>
        /// <param name="endpoints">
        /// List of endpoints to use for the initial
        /// connection and recovery.
        /// </param>
        /// <returns>Open connection</returns>
        /// <exception cref="BrokerUnreachableException">
        /// When no hostname was reachable.
        /// </exception>
        IConnection CreateConnection(IList<AmqpTcpEndpoint> endpoints);

        /// <summary>
        /// Amount of time protocol handshake operations are allowed to take before
        /// timing out.
        /// </summary>
        TimeSpan HandshakeContinuationTimeout { get; set; }

        /// <summary>
        /// Amount of time protocol  operations (e.g. <code>queue.declare</code>) are allowed to take before
        /// timing out.
        /// </summary>
        TimeSpan ContinuationTimeout { get; set; }
    }
}
