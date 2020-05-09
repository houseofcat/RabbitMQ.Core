using RabbitMQ.Client.Impl;
using System;
using System.Net.Sockets;

namespace RabbitMQ.Client
{
    public class ConnectionFactoryBase
    {
        /// <summary>
        /// Set custom socket options by providing a SocketFactory.
        /// </summary>
        public Func<AddressFamily, ITcpClient> SocketFactory = DefaultSocketFactory;

        /// <summary>
        /// Creates a new instance of the <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="addressFamily">Specifies the addressing scheme.</param>
        /// <returns>New instance of a <see cref="TcpClient"/>.</returns>
        public static ITcpClient DefaultSocketFactory(AddressFamily addressFamily)
        {
            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                ReceiveBufferSize = 65536,
                SendBufferSize = 65536
            };
            return new TcpClientAdapter(socket);
        }
    }
}
