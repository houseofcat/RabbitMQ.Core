using CookedRabbit.Core.Utils;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Pools
{
    public interface IConnectionPool
    {
        Config Config { get; }
        ConnectionFactory ConnectionFactory { get; set; }
        ulong CurrentConnectionId { get; }
        bool Initialized { get; }
        bool Shutdown { get; }


        ValueTask<IConnectionHost> GetConnectionAsync();
        Task InitializeAsync();
        Task ShutdownAsync();
    }

    public class ConnectionPool : IConnectionPool
    {
        private ILogger<ConnectionPool> _logger;

        public ConnectionFactory ConnectionFactory { get; set; }

        public ulong CurrentConnectionId { get; private set; }
        private Channel<IConnectionHost> Connections { get; set; }

        public bool Initialized { get; private set; }
        public bool Shutdown { get; private set; }
        private readonly SemaphoreSlim poolLock = new SemaphoreSlim(1, 1);

        public Config Config { get; }

        public ConnectionPool(Config config)
        {
            Guard.AgainstNull(config, nameof(config));
            Config = config;

            _logger = LogHelper.GetLogger<ConnectionPool>();

            Connections = Channel.CreateBounded<IConnectionHost>(Config.PoolSettings.MaxConnections);
            ConnectionFactory = CreateConnectionFactory();
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            var cf = new ConnectionFactory
            {
                Uri = Config.FactorySettings.Uri,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = Config.FactorySettings.TopologyRecovery,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(Config.FactorySettings.NetRecoveryTimeout),
                ContinuationTimeout = TimeSpan.FromSeconds(Config.FactorySettings.ContinuationTimeout),
                RequestedHeartbeat = TimeSpan.FromSeconds(Config.FactorySettings.HeartbeatInterval),
                RequestedChannelMax = Config.FactorySettings.MaxChannelsPerConnection,
                DispatchConsumersAsync = Config.FactorySettings.EnableDispatchConsumersAsync,
            };

            if (Config.FactorySettings.SslSettings.EnableSsl)
            {
                cf.Ssl = new SslOption
                {
                    Enabled = Config.FactorySettings.SslSettings.EnableSsl,
                    AcceptablePolicyErrors = Config.FactorySettings.SslSettings.AcceptedPolicyErrors,
                    ServerName = Config.FactorySettings.SslSettings.CertServerName,
                    CertPath = Config.FactorySettings.SslSettings.LocalCertPath,
                    CertPassphrase = Config.FactorySettings.SslSettings.LocalCertPassword,
                    Version = Config.FactorySettings.SslSettings.ProtocolVersions
                };
            }

            return cf;
        }

        public async Task InitializeAsync()
        {
            _logger.LogTrace(LogMessages.ConnectionPool.Initialization);

            await poolLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (!Initialized)
                {
                    await CreateConnectionsAsync()
                        .ConfigureAwait(false);

                    Initialized = true;
                    Shutdown = false;
                }
            }
            finally
            { poolLock.Release(); }

            _logger.LogTrace(LogMessages.ConnectionPool.Initialization);
        }

        private async Task CreateConnectionsAsync()
        {
            for (int i = 0; i < Config.PoolSettings.MaxConnections; i++)
            {
                var connectionName = $"{Config.PoolSettings.ConnectionPoolName}:{i}";
                try
                {
                    var connection = ConnectionFactory.CreateConnection();
                    await Connections
                        .Writer
                        .WriteAsync(new ConnectionHost(CurrentConnectionId++, connection));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, LogMessages.ConnectionPool.CreateConnectionException, connectionName);
                    throw; // Non Optional Throw
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<IConnectionHost> GetConnectionAsync()
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(ExceptionMessages.ValidationMessage);
            if (!await Connections
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.GetConnectionErrorMessage);
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
            if (!Initialized) throw new InvalidOperationException(ExceptionMessages.ShutdownValidationMessage);

            _logger.LogTrace(LogMessages.ConnectionPool.Shutdown);

            await poolLock
                .WaitAsync()
                .ConfigureAwait(false);

            if (!Shutdown)
            {
                await CloseConnectionsAsync()
                    .ConfigureAwait(false);

                Shutdown = true;
                Initialized = false;
            }

            poolLock.Release();

            _logger.LogTrace(LogMessages.ConnectionPool.ShutdownComplete);
        }

        private async Task CloseConnectionsAsync()
        {
            Connections.Writer.Complete();

            await Connections.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (Connections.Reader.TryRead(out IConnectionHost connHost))
            {
                try
                { connHost.Close(); }
                catch { }
            }
        }
    }
}
