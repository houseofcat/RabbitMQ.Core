using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Pools
{
    public interface IChannelHost
    {
        bool Ackable { get; }
        ulong ConnectionId { get; }
        ulong ChannelId { get; }
        bool Closed { get; }
        bool FlowControlled { get; }

        IModel GetChannel();
        Task<bool> MakeChannelAsync();
        void Close();
        Task<bool> HealthyAsync();
    }

    public class ChannelHost : IChannelHost
    {
        private readonly ILogger<ChannelHost> _logger;
        private IModel _channel { get; set; }
        private IConnectionHost ConnectionHost { get; }

        public ulong ChannelId { get; set; }
        public ulong ConnectionId { get; set; }

        public bool Ackable { get; }

        public bool Closed { get; private set; }
        public bool FlowControlled { get; private set; }

        private readonly SemaphoreSlim hostLock = new SemaphoreSlim(1, 1);

        public ChannelHost(ulong channelId, IConnectionHost connHost, bool ackable)
        {
            _logger = LogHelper.GetLogger<ChannelHost>();

            ChannelId = channelId;
            ConnectionHost = connHost;
            Ackable = ackable;

            MakeChannelAsync().GetAwaiter().GetResult();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IModel GetChannel()
        {
            hostLock.Wait();

            try
            {
                return _channel;
            }
            finally
            { hostLock.Release(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> MakeChannelAsync()
        {
            await hostLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_channel != null)
                {
                    _channel.FlowControl -= FlowControl;
                    _channel.ModelShutdown -= ChannelClose;
                    Close();
                    _channel = null;
                }

                _channel = ConnectionHost.Connection.CreateModel();

                if (Ackable)
                {
                    _channel.ConfirmSelect();
                }

                _channel.FlowControl += FlowControl;
                _channel.ModelShutdown += ChannelClose;

                return true;
            }
            catch
            {
                _channel = null;
                return false;
            }
            finally
            { hostLock.Release(); }
        }

        private void ChannelClose(object sender, ShutdownEventArgs e)
        {
            hostLock.Wait();
            _logger.LogDebug(e.ReplyText);
            Closed = true;
            hostLock.Release();
        }

        private void FlowControl(object sender, FlowControlEventArgs e)
        {
            hostLock.Wait();
            if (e.Active)
            { _logger.LogWarning(LogMessages.ChannelHost.FlowControlled); }
            else
            { _logger.LogInformation(LogMessages.ChannelHost.FlowControlFinished); }
            FlowControlled = e.Active;
            hostLock.Release();
        }

        public async Task<bool> HealthyAsync()
        {
            var connectionHealthy = await ConnectionHost.HealthyAsync().ConfigureAwait(false);

            return connectionHealthy && !FlowControlled && (_channel?.IsOpen ?? false);
        }

        private const int CloseCode = 200;
        private const string CloseMessage = "CookedRabbit manual close channel initiated.";

        public void Close()
        {
            if (!Closed || !_channel.IsOpen)
            {
                try
                { _channel.Close(CloseCode, CloseMessage); }
                catch { }
            }
        }
    }
}
