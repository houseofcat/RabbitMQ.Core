using CookedRabbit.Core.Configs;
using System.Collections.Generic;
using System.Threading.Channels;

namespace CookedRabbit.Core
{
    public class ConsumerOptions : GlobalConsumerOptions
    {
        public bool Enabled { get; set; }
        public string GlobalSettings { get; set; }
        public string ConsumerName { get; set; }

        public string QueueName { get; set; }
        public IDictionary<string, object> QueueArgs { get; set; }
        public string TargetQueueName { get; set; }
        public IDictionary<string, object> TargetQueueArgs { get; set; }

        public Dictionary<string, string> TargetQueues { get; set; } = new Dictionary<string, string>();

        public string ErrorQueueName => $"{QueueName}.{ErrorSuffix ?? "Error"}";
        public IDictionary<string, object> ErrorQueueArgs { get; set; }

        public string AltQueueName { get; set; }
        public IDictionary<string, object> AltQueueArgs { get; set; }
        public string AltQueueErrorName { get; set; }
        public IDictionary<string, object> AltQueueErrorArgs { get; set; }

        public ConsumerPipelineOptions ConsumerPipelineSettings { get; set; }
    }
}
