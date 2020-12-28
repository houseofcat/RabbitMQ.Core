using System;
using System.Collections.Generic;
using System.Text;

namespace CookedRabbit.Core.Configs
{
    public class GlobalConsumerPipelineOptions
    {
        public bool? WaitForCompletion { get; set; }
        public int? MaxDegreesOfParallelism { get; set; } = Environment.ProcessorCount;
        public bool? EnsureOrdered { get; set; }
    }
}
