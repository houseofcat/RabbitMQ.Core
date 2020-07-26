using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Pools
{
    public interface IConnectionHost
    {
        IConnection Connection { get; }
        ulong ConnectionId { get; }

        bool Blocked { get; }
        bool Dead { get; }
        bool Closed { get; }

        void AssignConnection(IConnection connection);
        void Close();
        Task<bool> HealthyAsync();
    }

    public class ConnectionHost : IConnectionHost
    {
        public IConnection Connection { get; private set; }
        public ulong ConnectionId { get; }

        public bool Blocked { get; private set; }
        public bool Dead { get; private set; }
        public bool Closed { get; private set; }

        private readonly ILogger<ConnectionHost> _logger;
        private readonly SemaphoreSlim hostLock = new SemaphoreSlim(1, 1);

        public ConnectionHost(ulong connectionId, IConnection connection)
        {
            _logger = LogHelper.GetLogger<ConnectionHost>();
            ConnectionId = connectionId;

            AssignConnection(connection);
        }

        public void AssignConnection(IConnection connection)
        {
            hostLock.Wait();

            if (Connection != null)
            {
                Connection.ConnectionBlocked -= ConnectionBlocked;
                Connection.ConnectionUnblocked -= ConnectionUnblocked;
                Connection.ConnectionShutdown -= ConnectionClosed;

                try
                { Close(); }
                catch { }

                Connection = null;
            }

            Connection = connection;

            Connection.ConnectionBlocked += ConnectionBlocked;
            Connection.ConnectionUnblocked += ConnectionUnblocked;
            Connection.ConnectionShutdown += ConnectionClosed;

            hostLock.Release();
        }

        private void ConnectionClosed(object sender, ShutdownEventArgs e)
        {
            hostLock.Wait();
            _logger.LogWarning(e.ReplyText);
            Closed = true;
            hostLock.Release();
        }

        private void ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            hostLock.Wait();
            _logger.LogWarning(e.Reason);
            Blocked = true;
            hostLock.Release();
        }

        private void ConnectionUnblocked(object sender, EventArgs e)
        {
            hostLock.Wait();
            _logger.LogInformation("Connection unblocked!");
            Blocked = false;
            hostLock.Release();
        }

        private const int CloseCode = 200;
        private const string CloseMessage = "CookedRabbit manual close initiated.";

        public void Close() => Connection.Close(CloseCode, CloseMessage);

        /// <summary>
        /// Due to the complexity of the RabbitMQ Dotnet Client there are a few odd scenarios.
        /// Just casually check Health() when looping through Connections, skip when not Healthy.
        /// <para>AutoRecovery = False yields results like Closed, Dead, and IsOpen will be true, true, false or false, false, true.</para>
        /// <para>AutoRecovery = True, yields difficult results like Closed, Dead, And IsOpen will be false, false, false or true, true, true (and other variations).</para>
        /// </summary>
        public async Task<bool> HealthyAsync()
        {
            await hostLock
                .WaitAsync()
                .ConfigureAwait(false);

            if (Closed && Connection.IsOpen)
            { Closed = false; } // Means a Recovery took place.
            else if (Dead && Connection.IsOpen)
            { Dead = false; } // Means a Miracle took place.

            hostLock.Release();

            return Connection.IsOpen && !Blocked; // TODO: See if we can incorporate Dead/Closed observations.
        }
    }
}
