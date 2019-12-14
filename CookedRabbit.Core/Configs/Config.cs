namespace CookedRabbit.Core.Configs
{
    /// <summary>
    /// Class to fully season RabbitServices to your taste!
    /// </summary>
    public class Config
    {
        /// <summary>
        /// RabbitMQ global consumer parameters.
        /// </summary>
        public ushort QosPrefetchSize { get; set; } = 0;

        /// <summary>
        /// RabbitMQ consumer parameters.
        /// <para>To fine tune, check consumer utilization located in RabbitMQ HTTP API management.</para>
        /// </summary>
        public ushort QosPrefetchCount { get; set; } = 120;

        /// <summary>
        /// Class to hold settings for ChannelFactory (RabbitMQ) settings.
        /// </summary>
        public FactoryOptions FactorySettings { get; set; } = new FactoryOptions();

        /// <summary>
        /// Class to hold settings for Channel/Connection pools.
        /// </summary>
        public PoolOptions PoolSettings { get; set; } = new PoolOptions();

        /// <summary>
        /// Class to hold settings for ChannelFactory/SSL (RabbitMQ) settings.
        /// </summary>
        public SslOptions SslSettings { get; set; } = new SslOptions();
    }
}
