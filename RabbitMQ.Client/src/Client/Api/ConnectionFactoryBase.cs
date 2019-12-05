using System;

#if NETFX_CORE
using Windows.Networking.Sockets;
#else
using System.Net.Sockets;
#endif

namespace RabbitMQ.Client
{
    public class ConnectionFactoryBase
    {
        /// <summary>
        /// Set custom socket options by providing a SocketFactory.
        /// </summary>
#if NETFX_CORE
        public Func<StreamSocket> SocketFactory = DefaultSocketFactory;
#else
        public Func<AddressFamily, ITcpClient> SocketFactory = DefaultSocketFactory;
#endif

#if NETFX_CORE
        /// <summary>
        /// Creates a new instance of the <see cref="StreamSocket"/>.
        /// </summary>
        /// <returns>New instance of a <see cref="StreamSocket"/>.</returns>
        public static StreamSocket DefaultSocketFactory()
        {
            StreamSocket tcpClient = new StreamSocket();
            tcpClient.Control.NoDelay = true;
            return tcpClient;
        }
#else
        /// <summary>
        /// Creates a new instance of the <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="addressFamily">Specifies the addressing scheme.</param>
        /// <returns>New instance of a <see cref="TcpClient"/>.</returns>
        public static ITcpClient DefaultSocketFactory(AddressFamily addressFamily)
        {
            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            return new TcpClientAdapter(socket);
        }
#endif
    }
}
