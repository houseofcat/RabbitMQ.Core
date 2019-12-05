using System;

namespace RabbitMQ.Client.Events
{
    ///<summary>Contains all the information about a message nack'd
    ///from an AMQP broker within the Basic content-class.</summary>
    public class BasicNackEventArgs : EventArgs
    {
        ///<summary>The sequence number of the nack'd message, or the
        ///closed upper bound of nack'd messages if multiple is
        ///true.</summary>
        public ulong DeliveryTag { get; set; }

        ///<summary>Whether this nack applies to one message or
        ///multiple messages.</summary>
        public bool Multiple { get; set; }

        ///<summary>Ignore</summary>
        ///<remarks>Clients should ignore this field.</remarks>
        public bool Requeue { get; set; }
    }
}
