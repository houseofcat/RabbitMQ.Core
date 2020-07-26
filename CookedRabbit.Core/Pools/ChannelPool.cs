using CookedRabbit.Core.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Pools
{
    public interface IChannelPool
    {
        Config Config { get; }
        ulong CurrentChannelId { get; }
        bool Initialized { get; }
        bool Shutdown { get; }

        /// <summary>
        /// This pulls an ackable <see cref="IChannelHost"/> out of the <see cref="IChannelPool"/> for usage.
        /// <para>If the <see cref="IChannelHost"/> was previously flagged on error, multi-attempts to recreate it before returning an open channel back to the user.
        /// If you only remove channels and never add them, you will drain your <see cref="IChannelPool"/>.</para>
        /// <para>Use <see cref="ReturnChannelAsync"/> to return Channels.</para>
        /// <para><em>Note: During an outage event, you will pause here until a viable channel can be acquired.</em></para>
        /// </summary>
        /// <returns><see cref="IChannelHost"/></returns>
        ValueTask<IChannelHost> GetAckChannelAsync();

        /// <summary>
        /// This pulls a <see cref="IChannelHost"/> out of the <see cref="IChannelPool"/> for usage.
        /// <para>If the <see cref="IChannelHost"/> was previously flagged on error, multi-attempts to recreate it before returning an open channel back to the user.
        /// If you only remove channels and never add them, you will drain your <see cref="IChannelPool"/>.</para>
        /// <para>Use <see cref="ReturnChannelAsync"/> to return the <see cref="IChannelHost"/>.</para>
        /// <para><em>Note: During an outage event, you will pause here until a viable channel can be acquired.</em></para>
        /// </summary>
        /// <returns><see cref="IChannelHost"/></returns>
        ValueTask<IChannelHost> GetChannelAsync();

        /// <summary>
        /// <para>Gives user a transient <see cref="IChannelHost"/> is simply a channel not managed by this library.</para>
        /// <para><em>Closing and disposing the <see cref="IChannelHost"/> is the responsiblity of the user.</em></para>
        /// </summary>
        /// <param name="ackable"></param>
        /// <returns><see cref="IChannelHost"/></returns>
        ValueTask<IChannelHost> GetTransientChannelAsync(bool ackable);
        Task InitializeAsync();
        ValueTask ReturnChannelAsync(IChannelHost chanHost, bool flagChannel = false);
        Task ShutdownAsync();
    }

    public class ChannelPool : IChannelPool
    {
        private readonly ILogger<ChannelPool> _logger;
        private readonly IConnectionPool _connectionPool;
        private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);
        private Channel<IChannelHost> _channels;
        private Channel<IChannelHost> _ackChannels;
        private ConcurrentDictionary<ulong, bool> _flaggedChannels { get; set; }

        public Config Config { get; }

        // A 0 indicates TransientChannels.
        public ulong CurrentChannelId { get; private set; } = 1;
        public bool Shutdown { get; private set; }
        public bool Initialized { get; private set; }

        public ChannelPool(Config config)
        {
            Guard.AgainstNull(config, nameof(config));

            Config = config;

            _logger = LogHelper.GetLogger<ChannelPool>();
            _connectionPool = new ConnectionPool(config);
            _flaggedChannels = new ConcurrentDictionary<ulong, bool>();
            _channels = Channel.CreateBounded<IChannelHost>(Config.PoolSettings.MaxChannels);
            _ackChannels = Channel.CreateBounded<IChannelHost>(Config.PoolSettings.MaxChannels);
        }

        public ChannelPool(ConnectionPool connPool)
        {
            Guard.AgainstNull(connPool, nameof(connPool));
            Config = connPool.Config;

            _logger = LogHelper.GetLogger<ChannelPool>();
            _connectionPool = connPool;
            _flaggedChannels = new ConcurrentDictionary<ulong, bool>();
            _channels = Channel.CreateBounded<IChannelHost>(Config.PoolSettings.MaxChannels);
            _ackChannels = Channel.CreateBounded<IChannelHost>(Config.PoolSettings.MaxChannels);
        }

        public async Task InitializeAsync()
        {
            _logger.LogTrace(LogMessages.ChannelPool.Initialization);

            await _poolLock
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (!Initialized)
                {
                    await CreateChannelsAsync()
                        .ConfigureAwait(false);

                    Initialized = true;
                    Shutdown = false;
                }
            }
            finally { _poolLock.Release(); }

            _logger.LogTrace(LogMessages.ChannelPool.InitializationComplete);
        }

        private async Task CreateChannelsAsync()
        {
            for (int i = 0; i < Config.PoolSettings.MaxChannels; i++)
            {
                var connHost = await _connectionPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                await _channels
                    .Writer
                    .WriteAsync(new ChannelHost(CurrentChannelId++, connHost, false));
            }

            for (int i = 0; i < Config.PoolSettings.MaxChannels; i++)
            {
                var connHost = await _connectionPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                await _ackChannels
                    .Writer
                    .WriteAsync(new ChannelHost(CurrentChannelId++, connHost, true));
            }
        }

        /// <summary>
        /// This pulls a <see cref="IChannelHost"/> out of the <see cref="IChannelPool"/> for usage.
        /// <para>If the <see cref="IChannelHost"/> was previously flagged on error, multi-attempta to recreate it before returning an open channel back to the user.
        /// If you only remove channels and never add them, you will drain your <see cref="IChannelPool"/>.</para>
        /// <para>Use <see cref="ReturnChannelAsync"/> to return the <see cref="IChannelHost"/>.</para>
        /// <para><em>Note: During an outage event, you will pause here until a viable channel can be acquired.</em></para>
        /// </summary>
        /// <returns><see cref="IChannelHost"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<IChannelHost> GetChannelAsync()
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);
            if (!await _channels
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelPoolGetChannelError);
            }

            var chanHost = await _channels
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);

            var healthy = await chanHost.HealthyAsync().ConfigureAwait(false);
            var flagged = _flaggedChannels.ContainsKey(chanHost.ChannelId) && _flaggedChannels[chanHost.ChannelId];
            if (flagged || !healthy)
            {
                _logger.LogWarning(LogMessages.ChannelPool.DeadChannel, chanHost.ChannelId);

                // Most likely this is closed, but if a user flags a healthy channel, the behavior implied/assumed
                // is they would like to replace it.
                chanHost.Close();

                chanHost = await CreateChannelAsync(chanHost.ChannelId, chanHost.Ackable)
                    .ConfigureAwait(false);
            }

            return chanHost;
        }

        /// <summary>
        /// This pulls an ackable <see cref="IChannelHost"/> out of the <see cref="IChannelPool"/> for usage.
        /// <para>If the <see cref="IChannelHost"/> was previously flagged on error, multi-attempta to recreate it before returning an open channel back to the user.
        /// If you only remove channels and never add them, you will drain your <see cref="IChannelPool"/>.</para>
        /// <para>Use <see cref="ReturnChannelAsync"/> to return Channels.</para>
        /// <para><em>Note: During an outage event, you will pause here until a viable channel can be acquired.</em></para>
        /// </summary>
        /// <returns><see cref="IChannelHost"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<IChannelHost> GetAckChannelAsync()
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);
            if (!await _ackChannels
                .Reader
                .WaitToReadAsync()
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException(ExceptionMessages.ChannelPoolGetChannelError);
            }

            var chanHost = await _ackChannels
                .Reader
                .ReadAsync()
                .ConfigureAwait(false);

            var healthy = await chanHost.HealthyAsync().ConfigureAwait(false);
            var flagged = _flaggedChannels.ContainsKey(chanHost.ChannelId) && _flaggedChannels[chanHost.ChannelId];
            if (flagged || !healthy)
            {
                _logger.LogWarning(LogMessages.ChannelPool.DeadChannel, chanHost.ChannelId);

                // Most likely this is closed, but if a user flags a healthy channel, the behavior implied/assumed
                // is they would like to replace it.
                chanHost.Close();

                chanHost = await CreateChannelAsync(chanHost.ChannelId, chanHost.Ackable)
                    .ConfigureAwait(false);
            }

            return chanHost;
        }

        /// <summary>
        /// <para>Gives user a transient <see cref="IChannelHost"/> is simply a channel not managed by this library.</para>
        /// <para><em>Closing and disposing the <see cref="IChannelHost"/> is the responsiblity of the user.</em></para>
        /// </summary>
        /// <param name="ackable"></param>
        /// <returns><see cref="IChannelHost"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<IChannelHost> GetTransientChannelAsync(bool ackable) => await CreateChannelAsync(0, ackable).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<IChannelHost> CreateChannelAsync(ulong channelId, bool ackable)
        {
            IChannelHost chanHost = null;
            IConnectionHost connHost = null;

            while (true)
            {
                _logger.LogDebug(LogMessages.ChannelPool.CreateChannel, channelId);

                var sleep = false;

                try
                {
                    connHost = await _connectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);

                    if (!await connHost.HealthyAsync().ConfigureAwait(false))
                    {
                        _logger.LogDebug(LogMessages.ChannelPool.CreateChannelFailedConnection, channelId);
                        sleep = true;
                    }
                }
                catch
                {
                    _logger.LogDebug(LogMessages.ChannelPool.CreateChannelFailedConnection, channelId);
                    sleep = true;
                }

                if (!sleep)
                {
                    try
                    { chanHost = new ChannelHost(channelId, connHost, ackable); }
                    catch
                    {
                        _logger.LogDebug(LogMessages.ChannelPool.CreateChannelFailedConstruction, channelId);
                        sleep = true;
                    }
                }

                if (sleep)
                {
                    _logger.LogDebug(LogMessages.ChannelPool.CreateChannelSleep, channelId);

                    await Task
                        .Delay(Config.PoolSettings.SleepOnErrorInterval)
                        .ConfigureAwait(false);

                    continue; // Continue here forever (till reconnection is established).
                }

                break;
            }

            _flaggedChannels[chanHost.ChannelId] = false;

            _logger.LogDebug(LogMessages.ChannelPool.CreateChannelSuccess, channelId);
            return chanHost;
        }

        /// <summary>
        /// Returns the <see cref="ChannelHost"/> back to the <see cref="ChannelPool"/>.
        /// <para>All Aqmp IModel Channels close server side on error, so you have to indicate to the library when that happens.</para>
        /// <para>The library does its best to listen for a dead <see cref="ChannelHost"/>, but nothing is as reliable as the user flagging the channel for replacement.</para>
        /// <para><em>Users flag the channel for replacement (e.g. when an error occurs) on it's next use.</em></para>
        /// </summary>
        /// <param name="chanHost"></param>
        /// <param name="flagChannel"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask ReturnChannelAsync(IChannelHost chanHost, bool flagChannel = false)
        {
            if (!Initialized || Shutdown) throw new InvalidOperationException(ExceptionMessages.ChannelPoolValidationMessage);

            _flaggedChannels[chanHost.ChannelId] = flagChannel;

            _logger.LogDebug(LogMessages.ChannelPool.ReturningChannel, chanHost.ChannelId, flagChannel);

            if (chanHost.Ackable)
            {
                await _ackChannels
                    .Writer
                    .WriteAsync(chanHost)
                    .ConfigureAwait(false);
            }
            else
            {
                await _channels
                    .Writer
                    .WriteAsync(chanHost)
                    .ConfigureAwait(false);
            }
        }

        public async Task ShutdownAsync()
        {
            if (!Initialized) throw new InvalidOperationException(ExceptionMessages.ChannelPoolShutdownValidationMessage);

            _logger.LogTrace(LogMessages.ChannelPool.Shutdown);

            await _poolLock
                .WaitAsync()
                .ConfigureAwait(false);

            if (!Shutdown)
            {
                await CloseChannelsAsync()
                    .ConfigureAwait(false);

                Shutdown = true;
                Initialized = false;

                await _connectionPool
                    .ShutdownAsync()
                    .ConfigureAwait(false);
            }

            _poolLock.Release();
            _logger.LogTrace(LogMessages.ChannelPool.ShutdownComplete);
        }

        private async Task CloseChannelsAsync()
        {
            // Signal to Channel no more data is coming.
            _channels.Writer.Complete();
            _ackChannels.Writer.Complete();

            await _channels.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_channels.Reader.TryRead(out IChannelHost chanHost))
            {
                try
                { chanHost.Close(); }
                catch { }
            }

            await _ackChannels.Reader.WaitToReadAsync().ConfigureAwait(false);
            while (_ackChannels.Reader.TryRead(out IChannelHost chanHost))
            {
                try
                { chanHost.Close(); }
                catch { }
            }
        }
    }
}
