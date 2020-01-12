namespace CookedRabbit.Core
{
    public class TopologyConfig
    {
        public ExchangeConfig[] ExchangeConfigs { get; set; }
        public QueueConfig[] QueueConfigs { get; set; }
        public ExchangeBindingConfig[] ExchangeBindingConfigs { get; set; }
        public QueueBindingConfig[] QueueBindingConfigs { get; set; }
    }
}
