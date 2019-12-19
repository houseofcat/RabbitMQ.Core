using System.Threading.Channels;

namespace CookedRabbit.Core
{
    public class PublisherOptions
    {
        public int LetterQueueBufferSize { get; set; } = 10_000;
        public int PriorityLetterQueueBufferSize { get; set; } = 100;
        public BoundedChannelFullMode BehaviorWhenFull { get; set; } = BoundedChannelFullMode.Wait;
        public int AutoPublisherSleepInterval { get; set; } = 1000;
    }
}
