using System;

#if !NETFX_CORE
using System.Net.Sockets;
#else
using Windows.Networking.Sockets;
#endif

using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Object describing various overarching parameters
    /// associated with a particular AMQP protocol variant.
    /// </summary>
    public interface IProtocol
    {
        /// <summary>
        /// Retrieve the protocol's API name, used for printing,
        /// configuration properties, IDE integration, Protocols.cs etc.
        /// </summary>
        string ApiName { get; }

        /// <summary>
        /// Retrieve the protocol's default TCP port.
        /// </summary>
        int DefaultPort { get; }

        /// <summary>
        /// Retrieve the protocol's major version number.
        /// </summary>
        int MajorVersion { get; }

        /// <summary>
        /// Retrieve the protocol's minor version number.
        /// </summary>
        int MinorVersion { get; }

        /// <summary>
        /// Retrieve the protocol's revision (if specified).
        /// </summary>
        int Revision { get; }

        /// <summary>
        /// Construct a connection from a given set of parameters,
        /// a frame handler, and no automatic recovery.
        /// The "insist" parameter is passed on to the AMQP connection.open method.
        /// </summary>
        IConnection CreateConnection(IConnectionFactory factory, bool insist, IFrameHandler frameHandler);

        /// <summary>
        /// Construct a connection from a given set of parameters,
        /// a frame handler, and automatic recovery settings.
        /// </summary>
        IConnection CreateConnection(ConnectionFactory factory, IFrameHandler frameHandler, bool automaticRecoveryEnabled);

        /// <summary>
        /// Construct a connection from a given set of parameters,
        /// a frame handler, a client-provided name, and no automatic recovery.
        /// The "insist" parameter is passed on to the AMQP connection.open method.
        /// </summary>
        IConnection CreateConnection(IConnectionFactory factory, bool insist, IFrameHandler frameHandler, String clientProvidedName);

        /// <summary>
        /// Construct a connection from a given set of parameters,
        /// a frame handler, a client-provided name, and automatic recovery settings.
        /// </summary>
        IConnection CreateConnection(ConnectionFactory factory, IFrameHandler frameHandler, bool automaticRecoveryEnabled, String clientProvidedName);

        /// <summary>
        /// Construct a protocol model atop a given session.
        /// </summary>
        IModel CreateModel(ISession session);
    }
}
