using System;

namespace RabbitMQ.Client.Events
{
    public sealed class QueueNameChangedAfterRecoveryEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueNameChangedAfterRecoveryEventArgs"/> class.
        /// </summary>
        /// <param name="nameBefore">The name before.</param>
        /// <param name="nameAfter">The name after.</param>
        public QueueNameChangedAfterRecoveryEventArgs(string nameBefore, string nameAfter)
        {
            NameBefore = nameBefore;
            NameAfter = nameAfter;
        }

        /// <summary>
        /// Gets the name before.
        /// </summary>
        public string NameBefore { get; }

        /// <summary>
        /// Gets the name after.
        /// </summary>
        public string NameAfter { get; }
    }
}
