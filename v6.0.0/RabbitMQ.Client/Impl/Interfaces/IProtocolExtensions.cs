using RabbitMQ.Client.Impl;
using System;
using System.Net.Sockets;

namespace RabbitMQ.Client.Framing.Impl
{
    public static class IProtocolExtensions
    {
        public static IFrameHandler CreateFrameHandler(
#pragma warning disable RCS1175 // Unused this parameter.
            this IProtocol protocol,
#pragma warning restore RCS1175 // Unused this parameter.
            AmqpTcpEndpoint endpoint,
            Func<AddressFamily, ITcpClient> socketFactory,
            TimeSpan connectionTimeout,
            TimeSpan readTimeout,
            TimeSpan writeTimeout)
        {
            return new SocketFrameHandler(endpoint, socketFactory, connectionTimeout, readTimeout, writeTimeout);
        }
    }
}
