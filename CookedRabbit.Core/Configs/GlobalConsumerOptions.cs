using CookedRabbit.Core.Configs;
using System.Collections.Generic;
using System.Threading.Channels;

namespace CookedRabbit.Core
{
    /// <summary>
    /// Global overrides for your consumers.
    /// </summary>
    public class GlobalConsumerOptions
    {
        public int ConsumerCount { get; set; } = 1;
        public bool? NoLocal { get; set; }
        public bool? Exclusive { get; set; }
        public ushort? BatchSize { get; set; } = 5;
        public bool? AutoAck { get; set; }
        public bool? UseTransientChannels { get; set; } = true;

        public string ErrorSuffix { get; set; }

        public BoundedChannelFullMode? BehaviorWhenFull { get; set; } = BoundedChannelFullMode.Wait;
        public int? SleepOnIdleInterval { get; set; } = 1000;

        public ConsumerPipelineOptions GlobalConsumerPipelineSettings { get; set; }
    }
}
