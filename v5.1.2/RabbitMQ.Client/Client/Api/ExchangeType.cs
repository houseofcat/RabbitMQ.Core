using System.Collections.Generic;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Convenience class providing compile-time names for standard exchange types.
    /// </summary>
    /// <remarks>
    /// Use the static members of this class as values for the
    /// "exchangeType" arguments for IModel methods such as
    /// ExchangeDeclare. The broker may be extended with additional
    /// exchange types that do not appear in this class.
    /// </remarks>
    public static class ExchangeType
    {
        /// <summary>
        /// Exchange type used for AMQP direct exchanges.
        /// </summary>
        public const string Direct = "direct";

        /// <summary>
        /// Exchange type used for AMQP fanout exchanges.
        /// </summary>
        public const string Fanout = "fanout";

        /// <summary>
        /// Exchange type used for AMQP headers exchanges.
        /// </summary>
        public const string Headers = "headers";

        /// <summary>
        /// Exchange type used for AMQP topic exchanges.
        /// </summary>
        public const string Topic = "topic";

        private static readonly string[] _all = { Fanout, Direct, Topic, Headers };

        /// <summary>
        /// Retrieve a collection containing all standard exchange types.
        /// </summary>
        public static ICollection<string> All()
        {
            return _all;
        }
    }
}
