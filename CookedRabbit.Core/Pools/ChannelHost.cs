using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace CookedRabbit.Core.Pools
{
    public class ChannelHost
    {
        public ulong ChannelId { get; set; }
        public ulong ConnectionId { get; set; }
        public IModel Channel { get; set; }
        public Func<Task<bool>> ConnectionHealthy;
        private readonly SemaphoreSlim hostLock = new SemaphoreSlim(1, 1);

        public bool Ackable { get; }
        public bool Closed { get; private set; }

        public ChannelHost(ulong channelId, ConnectionHost connHost, bool ackable)
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
            hostLock.Wait();
            Closed = true;
            hostLock.Release();
        }

        public async Task<bool> HealthyAsync()
        {
            var connHealthy = await ConnectionHealthy()
                .ConfigureAwait(false);

            return connHealthy && !Closed && Channel.IsOpen;
        }

        private const int CloseCode = 200;
        private const string CloseMessage = "Manual close channel initiated.";

        public void Close() => Channel.Close(CloseCode, CloseMessage);
    }
}
