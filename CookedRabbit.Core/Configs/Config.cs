using System;
using System.Collections.Generic;
using System.Globalization;

namespace CookedRabbit.Core
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
        public IDictionary<string, ConsumerOptions> LetterConsumerSettings { get; set; } = new Dictionary<string, ConsumerOptions>();
        public IDictionary<string, ConsumerOptions> MessageConsumerSettings { get; set; } = new Dictionary<string, ConsumerOptions>();

        public ConsumerOptions GetLetterConsumerSettings(string consumerName)
        {
            if (!LetterConsumerSettings.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Strings.NoConsumerSettingsMessage, consumerName));
            return LetterConsumerSettings[consumerName];
        }

        public ConsumerOptions GetMessageConsumerSettings(string consumerName)
        {
            if (!MessageConsumerSettings.ContainsKey(consumerName)) throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Strings.NoConsumerSettingsMessage, consumerName));
            return MessageConsumerSettings[consumerName];
        }
    }
}
