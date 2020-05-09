using System;

namespace RabbitMQ.Client.Events
{
    /// <summary>
    /// Event relating to connection being blocked.
    /// </summary>
    public class ConnectionBlockedEventArgs : EventArgs
    {
        public ConnectionBlockedEventArgs(string reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// Access the reason why connection is blocked.
        /// </summary>
        public string Reason { get; private set; }
    }
}
