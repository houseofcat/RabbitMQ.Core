using RabbitMQ.Client.Impl;
using System;
using System.Net.Sockets;

namespace RabbitMQ.Client.Framing.Impl
{
    internal static class IProtocolExtensions
    {
        public static IFrameHandler CreateFrameHandler(
            this IProtocol protocol,
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
