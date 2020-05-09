using System;

namespace RabbitMQ.Client.Events
{
    ///<summary>Event relating to a successful consumer registration
    ///or cancellation.</summary>
    public class ConsumerEventArgs : EventArgs
    {
        ///<summary>Construct an event containing the consumer-tags of
        ///the consumer the event relates to.</summary>
        public ConsumerEventArgs(string[] consumerTags)
        {
            ConsumerTags = consumerTags;
        }

        ///<summary>Access the consumer-tags of the consumer the event
        ///relates to.</summary>
        public string[] ConsumerTags { get; }
    }
}
