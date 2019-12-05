using System;
using System.Net.Sockets;

using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.Framing.Impl
{
    public static class IProtocolExtensions
    {
        public static IFrameHandler CreateFrameHandler(
#pragma warning disable RCS1175 // Unused this parameter.
#pragma warning disable IDE0060 // Remove unused parameter
            this IProtocol protocol,
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1175 // Unused this parameter.
            AmqpTcpEndpoint endpoint,
            Func<AddressFamily, ITcpClient> socketFactory,
            int connectionTimeout,
            int readTimeout,
            int writeTimeout)
        {
            return new SocketFrameHandler(endpoint, socketFactory,
                connectionTimeout, readTimeout, writeTimeout);
        }
    }
}