using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Pools
{
    public interface IChannelHost
    {
        bool Ackable { get; }
        IModel Channel { get; set; }
        ulong ChannelId { get; set; }
        bool Closed { get; }
        ulong ConnectionId { get; set; }

        void Close();
        Task<bool> HealthyAsync();
    }

    public class ChannelHost : IChannelHost
    {
        public ulong ChannelId { get; set; }
        public ulong ConnectionId { get; set; }
        public IModel Channel { get; set; }
        public Func<Task<bool>> ConnectionHealthy;

        public bool Ackable { get; }
        public bool Closed { get; private set; }

        public ChannelHost(ulong channelId, IConnectionHost connHost, bool ackable)
        {
            ChannelId = channelId;
            Channel = connHost
                .Connection
                .CreateModel();

            if (ackable)
            {
                Ackable = ackable;
                Channel.ConfirmSelect();
            }

            Channel.ModelShutdown += ChannelClose;
            ConnectionHealthy = connHost.HealthyAsync;
        }

        private void ChannelClose(object sender, ShutdownEventArgs e)
        {
            Closed = true;
        }

        public async Task<bool> HealthyAsync()
        {
            var connHealthy = await ConnectionHealthy().ConfigureAwait(false);

            return connHealthy && !Closed && Channel.IsOpen;
        }

        private const int CloseCode = 200;
        private const string CloseMessage = "Manual close channel initiated.";

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
