using System.Collections.Generic;

namespace CookedRabbit.Core.Configs
{
    public class Config
    {
        /// <summary>
        /// Class to hold settings for ConnectionFactory (RabbitMQ) options.
        /// </summary>
        public FactoryOptions FactorySettings { get; set; } = new FactoryOptions();

        /// <summary>
        /// Class to hold settings for Channel/ConnectionPool options.
        /// </summary>
        public PoolOptions PoolSettings { get; set; } = new PoolOptions();

        /// <summary>
        /// Class to hold settings for Publisher/AutoPublisher options.
        /// </summary>
        public PublisherOptions PublisherSettings { get; set; } = new PublisherOptions();

        /// <summary>
        /// Dictionary to hold all the ConsumerSettings using the ConsumerOption class.
        /// </summary>
        public IDictionary<string, ConsumerOptions> ConsumerSettings { get; set; } = new Dictionary<string, ConsumerOptions>();
    }
}
