using System;

namespace RabbitMQ.Client.Events
{
    /// <summary>
    /// Event relating to flow control.
    /// </summary>
    public class FlowControlEventArgs : EventArgs
    {
        public FlowControlEventArgs(bool active)
        {
            Active = active;
        }

        /// <summary>
        /// Access the flow control setting.
        /// </summary>
        public bool Active { get; private set; }
    }
}
