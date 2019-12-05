using System;

namespace RabbitMQ.Client.Events
{
    public sealed class ConsumerTagChangedAfterRecoveryEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerTagChangedAfterRecoveryEventArgs"/> class.
        /// </summary>
        /// <param name="tagBefore">The tag before.</param>
        /// <param name="tagAfter">The tag after.</param>
        public ConsumerTagChangedAfterRecoveryEventArgs(string tagBefore, string tagAfter)
        {
            TagBefore = tagBefore;
            TagAfter = tagAfter;
        }

        /// <summary>
        /// Gets the tag before.
        /// </summary>
        public string TagBefore { get; }

        /// <summary>
        /// Gets the tag after.
        /// </summary>
        public string TagAfter { get; }
    }
}
