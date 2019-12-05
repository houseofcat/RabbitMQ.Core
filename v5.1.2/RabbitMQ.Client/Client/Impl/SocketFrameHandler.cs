using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    internal static class TaskExtensions
    {
        public static async Task TimeoutAfter(this Task task, int millisecondsTimeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)).ConfigureAwait(false))
            {
                await task.ConfigureAwait(false);
            }
            else
            {
                var supressErrorTask = task.ContinueWith(t => t.Exception.Handle(_ => true), TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException();
            }
        }
    }

    public class SocketFrameHandler : IFrameHandler
    {
        // Timeout in seconds to wait for a clean socket close.
        // Socket poll timeout in ms. If the socket does not
        // become writeable in this amount of time, we throw
        // an exception.
        private int m_writeableStateTimeout = 30000;
        private readonly NetworkBinaryReader m_reader;
        private readonly ITcpClient m_socket;
        private readonly NetworkBinaryWriter m_writer;
        private readonly object _semaphore = new object();
        private readonly object _sslStreamLock = new object();
        private bool _closed;
        private readonly bool _ssl;

        public SocketFrameHandler(AmqpTcpEndpoint endpoint,
            Func<AddressFamily, ITcpClient> socketFactory,
            int connectionTimeout, int readTimeout, int writeTimeout)
        {
            Endpoint = endpoint;

            if (ShouldTryIPv6(endpoint))
            {
                try
                {
                    m_socket = ConnectUsingIPv6(endpoint, socketFactory, connectionTimeout);
                }
                catch (ConnectFailureException)
                {
                    m_socket = null;
                }
            }

            if (m_socket == null && endpoint.AddressFamily != AddressFamily.InterNetworkV6)
            {
                m_socket = ConnectUsingIPv4(endpoint, socketFactory, connectionTimeout);
            }

            Stream netstream = m_socket.GetStream();
            netstream.ReadTimeout = readTimeout;
            netstream.WriteTimeout = writeTimeout;

            if (endpoint.Ssl.Enabled)
            {
                try
                {
                    netstream = SslHelper.TcpUpgrade(netstream, endpoint.Ssl);
                    _ssl = true;
                }
                catch (Exception)
                {
                    Close();
                    throw;
                }
            }
            m_reader = new NetworkBinaryReader(new BufferedStream(netstream, m_socket.Client.ReceiveBufferSize));
            m_writer = new NetworkBinaryWriter(netstream);

            m_writeableStateTimeout = writeTimeout;
        }

        public AmqpTcpEndpoint Endpoint { get; set; }

        public EndPoint LocalEndPoint
        {
            get { return m_socket.Client.LocalEndPoint; }
        }

        public int LocalPort
        {
            get { return ((IPEndPoint)LocalEndPoint).Port; }
        }

        public EndPoint RemoteEndPoint
        {
            get { return m_socket.Client.RemoteEndPoint; }
        }

        public int RemotePort
        {
            get { return ((IPEndPoint)LocalEndPoint).Port; }
        }

        public int ReadTimeout
        {
            set
            {
                try
                {
                    if (m_socket.Connected)
                    {
                        m_socket.ReceiveTimeout = value;
                    }
                }
                catch (SocketException)
                {
                    // means that the socket is already closed
                }
            }
        }

        public int WriteTimeout
        {
            set
            {
                m_writeableStateTimeout = value;
                m_socket.Client.SendTimeout = value;
            }
        }

        public void Close()
        {
            lock (_semaphore)
            {
                if (!_closed)
                {
                    try
                    {
                        m_socket.Close();
                    }
                    catch
                    {
                        // ignore, we are closing anyway
                    }
                    finally
                    {
                        _closed = true;
                    }
                }
            }
        }

        public InboundFrame ReadFrame()
        {
            return InboundFrame.ReadFrom(m_reader);
        }

        private static readonly byte[] amqp = Encoding.ASCII.GetBytes("AMQP");
        private const byte one = 1;

        public void SendHeader()
        {
            var ms = new MemoryStream();
            var nbw = new NetworkBinaryWriter(ms);
            nbw.Write(amqp);

            if (Endpoint.Protocol.Revision != 0)
            {
                nbw.Write((byte)0);
                nbw.Write((byte)Endpoint.Protocol.MajorVersion);
                nbw.Write((byte)Endpoint.Protocol.MinorVersion);
                nbw.Write((byte)Endpoint.Protocol.Revision);
            }
            else
            {
                nbw.Write(one);
                nbw.Write(one);
                nbw.Write((byte)Endpoint.Protocol.MajorVersion);
                nbw.Write((byte)Endpoint.Protocol.MinorVersion);
            }
            Write(ms.ToArray());
        }

        public void WriteFrame(OutboundFrame frame)
        {
            var ms = new MemoryStream();
            var nbw = new NetworkBinaryWriter(ms);
            frame.WriteTo(nbw);
            m_socket.Client.Poll(m_writeableStateTimeout, SelectMode.SelectWrite);
            Write(ms.ToArray());
        }

        public void WriteFrameSet(IList<OutboundFrame> frames)
        {
            var ms = new MemoryStream();
            var nbw = new NetworkBinaryWriter(ms);
            foreach (var f in frames)
            {
                f.WriteTo(nbw);
            }

            m_socket.Client.Poll(m_writeableStateTimeout, SelectMode.SelectWrite);
            Write(ms.ToArray());
        }

        private void Write(byte[] buffer)
        {
            if (_ssl)
            {
                lock (_sslStreamLock)
                {
                    m_writer.Write(buffer);
                }
            }
            else
            {
                m_writer.Write(buffer);
            }
        }

        private bool ShouldTryIPv6(AmqpTcpEndpoint endpoint)
        {
            return Socket.OSSupportsIPv6 && endpoint.AddressFamily != AddressFamily.InterNetwork;
        }

        private ITcpClient ConnectUsingIPv6(AmqpTcpEndpoint endpoint,
                                            Func<AddressFamily, ITcpClient> socketFactory,
                                            int timeout)
        {
            return ConnectUsingAddressFamily(endpoint, socketFactory, timeout, AddressFamily.InterNetworkV6);
        }

        private ITcpClient ConnectUsingIPv4(AmqpTcpEndpoint endpoint,
                                            Func<AddressFamily, ITcpClient> socketFactory,
                                            int timeout)
        {
            return ConnectUsingAddressFamily(endpoint, socketFactory, timeout, AddressFamily.InterNetwork);
        }

        private ITcpClient ConnectUsingAddressFamily(AmqpTcpEndpoint endpoint,
                                                    Func<AddressFamily, ITcpClient> socketFactory,
                                                    int timeout, AddressFamily family)
        {
            ITcpClient socket = socketFactory(family);
            try
            {
                ConnectOrFail(socket, endpoint, timeout);
                return socket;
            }
            catch (ConnectFailureException)
            {
                socket.Dispose();
                throw;
            }
        }

        private void ConnectOrFail(ITcpClient socket, AmqpTcpEndpoint endpoint, int timeout)
        {
            try
            {
                socket.ConnectAsync(endpoint.HostName, endpoint.Port)
                      .TimeoutAfter(timeout)
                      .ConfigureAwait(false)
                      // this ensures exceptions aren't wrapped in an AggregateException
                      .GetAwaiter()
                      .GetResult();
            }
            catch (ArgumentException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (SocketException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (NotSupportedException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
            catch (TimeoutException e)
            {
                throw new ConnectFailureException("Connection failed", e);
            }
        }
    }
}
