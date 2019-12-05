using System;

namespace RabbitMQ.Client.Events
{
    ///<summary>Event relating to a successful consumer registration
    ///or cancellation.</summary>
    public class ConsumerEventArgs : EventArgs
    {
        ///<summary>Construct an event containing the consumer-tag of
        ///the consumer the event relates to.</summary>
        public ConsumerEventArgs(string consumerTag)
        {
            ConsumerTag = consumerTag;
        }

        ///<summary>Access the consumer-tag of the consumer the event
        ///relates to.</summary>
        public string ConsumerTag { get; }
    }
}
