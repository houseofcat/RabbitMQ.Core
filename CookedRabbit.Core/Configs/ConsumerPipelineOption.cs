using System;
using System.Collections.Generic;
using System.Text;

namespace CookedRabbit.Core.Configs
{
    public class ConsumerPipelineOption
    {
        public string ConsumerPipelineName { get; set; }
        public bool? WaitForCompletion { get; set; }
        public int? MaxDegreesOfParallelism { get; set; } = Environment.ProcessorCount;
    }
}
