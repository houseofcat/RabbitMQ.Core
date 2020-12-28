using RabbitMQ.Client;
using System.Threading;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Pools
{
    public interface IConnectionHost
    {
        bool Closed { get; }
        IConnection Connection { get; set; }
        ulong ConnectionId { get; set; }
        bool Dead { get; }

        void Close();
        Task<bool> HealthyAsync();
    }

    public class ConnectionHost : IConnectionHost
    {
        public ulong ConnectionId { get; set; }
        public IConnection Connection { get; set; }
        private readonly SemaphoreSlim hostLock = new SemaphoreSlim(1, 1);

        public ConnectionHost(ulong connectionId, IConnection connection)
        {
            ConnectionId = connectionId;
            Connection = connection;

            Connection.ConnectionShutdown += ConnectionClosed;
        }

        public bool Dead { get; private set; }

        public bool Closed { get; private set; }

        private void ConnectionClosed(object sender, ShutdownEventArgs e)
        {
            hostLock.Wait();
            Closed = true;
            hostLock.Release();
        }

        private const int CloseCode = 200;
        private const string CloseMessage = "Manual close initiated.";

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

            return Connection.IsOpen; // TODO: See if we can incorporate Dead/Closed observations.
        }
    }
}
