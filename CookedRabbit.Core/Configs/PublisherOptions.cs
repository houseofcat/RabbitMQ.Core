using System.Threading.Channels;

namespace CookedRabbit.Core
{
    public class PublisherOptions
    {
        public bool CreatePublishReceipts { get; set; }
        public int LetterQueueBufferSize { get; set; } = 10_000;
        public int PriorityLetterQueueBufferSize { get; set; } = 100;
        public BoundedChannelFullMode BehaviorWhenFull { get; set; } = BoundedChannelFullMode.Wait;
        //public int AutoPublisherSleepInterval { get; set; } = 1000;

        public bool Compress { get; set; }
        public bool Encrypt { get; set; }
        public bool WithHeaders { get; set; } = true;
    }
}
