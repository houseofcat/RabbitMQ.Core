using System;

namespace RabbitMQ.Client.Events
{
    ///<summary>Contains all the information about a message acknowledged
    ///from an AMQP broker within the Basic content-class.</summary>
    public class BasicAckEventArgs : EventArgs
    {
        ///<summary>The sequence number of the acknowledged message, or
        ///the closed upper bound of acknowledged messages if multiple
        ///is true.</summary>
        public ulong DeliveryTag { get; set; }

        ///<summary>Whether this acknoledgement applies to one message
        ///or multiple messages.</summary>
        public bool Multiple { get; set; }
    }
}
