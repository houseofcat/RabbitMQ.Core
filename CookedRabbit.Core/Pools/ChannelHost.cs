using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Pools
{
    public interface IChannelHost
    {
        bool Ackable { get; }
        IModel Channel { get; }
        ulong ConnectionId { get; }

        ulong ChannelId { get; }
        bool Closed { get; }
        bool FlowControlled { get; }

        bool MakeChannel();
        void Close();
        Task<bool> HealthyAsync();
    }

    public class ChannelHost : IChannelHost
    {
        private ILogger<ChannelHost> _logger;
        public IModel Channel { get; private set; }
        private IConnectionHost ConnectionHost { get; set; }

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

            MakeChannel();
        }

        public bool MakeChannel()
        {
            hostLock.Wait();

            try
            {
                if (Channel != null)
                {
                    Channel.FlowControl -= FlowControl;
                    Channel.ModelShutdown -= ChannelClose;
                    Close();
                    Channel = null;
                }

                Channel = ConnectionHost.Connection.CreateModel();

                if (Ackable)
                {
                    Channel.ConfirmSelect();
                }

                Channel.FlowControl += FlowControl;
                Channel.ModelShutdown += ChannelClose;

                return true;
            }
            catch
            { return false; }
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

            return connectionHealthy && !FlowControlled && Channel.IsOpen;
        }

        private const int CloseCode = 200;
        private const string CloseMessage = "CookedRabbit manual close channel initiated.";

        public void Close()
        {
            if (!Closed || !Channel.IsOpen)
            {
                try
                { Channel.Close(CloseCode, CloseMessage); }
                catch { }
            }
        }
    }
}
