using RabbitMQ.Client.Framing;

namespace RabbitMQ.Client
{
    ///<summary>
    /// Provides access to the supported <see cref="IProtocol"/> implementations.
    /// </summary>
    public static class Protocols
    {
        ///<summary>
        /// Protocol version 0-9-1 as modified by Pivotal.
        ///</summary>
        public static IProtocol AMQP_0_9_1
        {
            get { return new Protocol(); }
        }

        ///<summary>
        /// Retrieve the current default protocol variant (currently AMQP_0_9_1).
        ///</summary>
        public static IProtocol DefaultProtocol
        {
            get { return AMQP_0_9_1; }
        }
    }
}
