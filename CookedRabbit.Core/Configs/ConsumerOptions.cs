namespace CookedRabbit.Core.Configs
{
    public class ConsumerOptions
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
    }
}
