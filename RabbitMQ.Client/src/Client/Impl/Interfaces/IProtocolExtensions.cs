using System;

#if !NETFX_CORE
using System.Net.Sockets;
#else
using Windows.Networking.Sockets;
#endif

using RabbitMQ.Client.Impl;

namespace RabbitMQ.Client.Framing.Impl
{
    public static class IProtocolExtensions
    {
        public static IFrameHandler CreateFrameHandler(
            this IProtocol protocol,
            AmqpTcpEndpoint endpoint,
#if !NETFX_CORE
            Func<AddressFamily, ITcpClient> socketFactory,
#else
            Func<StreamSocket> socketFactory,
#endif
            int connectionTimeout,
            int readTimeout,
            int writeTimeout)
        {
            return new SocketFrameHandler(endpoint, socketFactory,
                connectionTimeout, readTimeout, writeTimeout);
        }
    }
}