using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CookedRabbit.Core.Configs;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace CookedRabbit.Core.Pools
{
    public class ConnectionPool
    {
        public ConnectionFactory ConnectionFactory { get; set; }

        public ulong CurrentConnectionId { get; private set; }
        private Channel<ConnectionHost> Connections { get; set; }

        public bool Initialized { get; private set; }
        public bool Shutdown { get; private set; }
        private readonly SemaphoreSlim poolLock = new SemaphoreSlim(1, 1);

        public Config Config { get; }

        private const string ValidationMessage = "ConnectionPool is not initialized or is shutdown.";
        private const string ShutdownValidationMessage = "ConnectionPool is not initialized. Can't be Shutdown.";
        private const string GetConnectionError = "Threading.Channel used for reading RabbitMQ connections has been closed.";

        public ConnectionPool(Config config)
        {
            Config = config;
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            var cf = new ConnectionFactory
            {
                Uri = Config.FactorySettings.Uri,
                AutomaticRecoveryEnabled = Config.FactorySettings.AutoRecovery,
                TopologyRecoveryEnabled = Config.FactorySettings.TopologyRecovery,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(Config.FactorySettings.NetRecoveryTimeout),
                ContinuationTimeout = TimeSpan.FromSeconds(Config.FactorySettings.ContinuationTimeout),
                RequestedHeartbeat = Config.FactorySettings.HeartbeatInterval,
                RequestedChannelMax = Config.FactorySettings.MaxChannelsPerConnection,
                DispatchConsumersAsync = Config.FactorySettings.EnableDispatchConsumersAsync,
                UseBackgroundThreadsForIO = Config.FactorySettings.UseBackgroundThreadsForIO,
            };

            if (Config.SslSettings.EnableSsl)
            {
                cf.Ssl = new SslOption
                {
                    Enabled = Config.SslSettings.EnableSsl,
                    AcceptablePolicyErrors = Config.SslSettings.AcceptedPolicyErrors,
                    ServerName = Config.SslSettings.CertServerName,
                    CertPath = Config.SslSettings.LocalCertPath,
                    CertPassphrase = Config.SslSettings.LocalCertPassword,
                    Version = Config.SslSettings.ProtocolVersions
                };
            }

            return cf;
        }

        public async Task InitializeAsync()
        {
            await poolLock
                .WaitAsync()
                .ConfigureAwait(false);

            if (!Initialized)
            {
                ConfigurePool();

                await CreateConnectionsAsync()
                    .ConfigureAwait(false);

                Initialized = true;
                Shutdown = false;
            }

            poolLock.Release();
        }

        public void ConfigurePool()
        {
            Connections = Channel.CreateBounded<ConnectionHost>(Config.PoolSettings.MaxConnections);
            ConnectionFactory = CreateConnectionFactory();
        }

        private async Task CreateConnectionsAsync()
        {
            for (int i = 0; i < Config.PoolSettings.MaxConnections; i++)
            {
                try
                {
                    await Connections
                        .Writer
                        .WriteAsync(
                            new ConnectionHost(
                                CurrentConnectionId++,
                                ConnectionFactory.CreateConnection($"{Config.PoolSettings.ConnectionPoolName}:{i}"))
                         );
                }
                catch (BrokerUnreachableException)
                {
                    // TODO: Implement Logger
                    // RabbitMQ Server/Cluster is unreachable.
                    throw; // Non Optional Throw
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<ConnectionHost> GetConnectionAsync()
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(ValidationMessage);
            if (!await Connections
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(GetConnectionError);
            }

            var connHost = await Connections
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);

            await Connections
                .Writer
                .WriteAsync(connHost)
                .ConfigureAwait(false);

            return connHost;
        }

        public async Task ShutdownAsync()
        {
            if (!Initialized) throw new InvalidOperationException(ShutdownValidationMessage);

            await poolLock
                .WaitAsync()
                .ConfigureAwait(false);

            if (!Shutdown)
            {
                await CloseConnectionsAsync()
                    .ConfigureAwait(false);

                Shutdown = true;
                Initialized = false;
                poolLock.Release();
            }
        }

        private async Task CloseConnectionsAsync()
        {
            Connections.Writer.Complete();
#if CORE3
            await foreach (var connHost in Connections.Reader.ReadAllAsync())
#elif CORE2
            await Connections.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (Connections.Reader.TryRead(out ConnectionHost connHost))
#endif
            {
                try
                { connHost.Close(); }
                catch { }
            }
        }
    }
}
