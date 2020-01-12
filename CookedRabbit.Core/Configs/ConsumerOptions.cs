using System.Threading.Channels;

namespace CookedRabbit.Core
{
    public class ConsumerOptions
    {
        public string QueueName { get; set; }
        public string ConsumerName { get; set; }
        public bool NoLocal { get; set; }
        public bool Exclusive { get; set; }
        public ushort QosPrefetchCount { get; set; } = 5;

        public int MessageBufferSize { get; set; } = 100;
        public BoundedChannelFullMode BehaviorWhenFull { get; set; } = BoundedChannelFullMode.Wait;
    }
}
