namespace CookedRabbit.Core.Configs
{
    /// <summary>
    /// Class to fully season RabbitServices to your taste!
    /// </summary>
    public class ApiOptions
    {
        /// <summary>
        /// Service will setup communication for Rabbit HTTP API communication.
        /// </summary>
        public bool RabbitApiAccessEnabled { get; set; } = true;

        /// <summary>
        /// Whether or not to use SSL (HTTPS) for API path construction.
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// Rabbit HTTP API host name.
        /// </summary>
        public string RabbitApiHostName { get; set; } = "localhost";

        /// <summary>
        /// User to use when communicating with Rabbit HTTP API host.
        /// </summary>
        public string RabbitApiUserName { get; set; } = "guest";

        /// <summary>
        /// Password to use when communicating with Rabbit HTTP API host.
        /// </summary>
        public string RabbitApiUserPassword { get; set; } = "guest";

        /// <summary>
        /// Port to use when communicating with Rabbit HTTP API host.
        /// </summary>
        public int RabbitApiPort { get; set; } = 15672;

        /// <summary>
        /// Timeout for Rabbit HTTP API calls (in seconds).
        /// </summary>
        public int RabbitApiTimeout { get; set; } = 60;
    }
}
