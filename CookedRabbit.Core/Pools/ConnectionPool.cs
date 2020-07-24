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
        bool Initialized { get; }
        bool Shutdown { get; }

        IConnection CreateConnection(string connectionName);
        ValueTask<IConnectionHost> GetConnectionAsync();
        Task InitializeAsync();
        Task ShutdownAsync();
    }

    public class ConnectionPool : IConnectionPool
    {
        private readonly ILogger<ConnectionPool> _logger;
        private readonly Channel<IConnectionHost> _connections;
        private readonly ConnectionFactory _connectionFactory;
        private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);
        private ulong _currentConnectionId { get; set; }

        public bool Initialized { get; private set; }
        public bool Shutdown { get; private set; }
        public Config Config { get; }

        public ConnectionPool(Config config)
        {
            Guard.AgainstNull(config, nameof(config));
            Config = config;

            _logger = LogHelper.GetLogger<ConnectionPool>();

            _connections = Channel.CreateBounded<IConnectionHost>(Config.PoolSettings.MaxConnections);
            _connectionFactory = CreateConnectionFactory();
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

        public IConnection CreateConnection(string connectionName) => _connectionFactory.CreateConnection(connectionName);

        public async Task InitializeAsync()
        {
            _logger.LogTrace(LogMessages.ConnectionPool.Initialization);

            await _poolLock
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
            { _poolLock.Release(); }

            _logger.LogTrace(LogMessages.ConnectionPool.Initialization);
        }

        private async Task CreateConnectionsAsync()
        {
            for (int i = 0; i < Config.PoolSettings.MaxConnections; i++)
            {
                var serviceName = string.IsNullOrEmpty(Config.PoolSettings.ServiceName) ? $"CookedRabbit:{i}" : $"{Config.PoolSettings.ServiceName}:{i}";
                try
                {
                    var connection = _connectionFactory.CreateConnection(serviceName);
                    await _connections
                        .Writer
                        .WriteAsync(new ConnectionHost(_currentConnectionId++, connection));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, LogMessages.ConnectionPool.CreateConnectionException, serviceName);
                    throw; // Non Optional Throw
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<IConnectionHost> GetConnectionAsync()
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(ExceptionMessages.ValidationMessage);
            if (!await _connections
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.GetConnectionErrorMessage);
            }

            var connHost = await _connections
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);

            await _connections
                .Writer
                .WriteAsync(connHost)
                .ConfigureAwait(false);

            return connHost;
        }

        public async Task ShutdownAsync()
        {
            if (!Initialized) throw new InvalidOperationException(ExceptionMessages.ShutdownValidationMessage);

            _logger.LogTrace(LogMessages.ConnectionPool.Shutdown);

            await _poolLock
                .WaitAsync()
                .ConfigureAwait(false);

            if (!Shutdown)
            {
                await CloseConnectionsAsync()
                    .ConfigureAwait(false);

                Shutdown = true;
                Initialized = false;
            }

            _poolLock.Release();

            _logger.LogTrace(LogMessages.ConnectionPool.ShutdownComplete);
        }

        private async Task CloseConnectionsAsync()
        {
            _connections.Writer.Complete();

            await _connections.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_connections.Reader.TryRead(out IConnectionHost connHost))
            {
                try
                { connHost.Close(); }
                catch { }
            }
        }
    }
}
