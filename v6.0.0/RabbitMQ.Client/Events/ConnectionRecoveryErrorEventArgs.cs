using System;

namespace RabbitMQ.Client.Events
{
    public sealed class ConnectionRecoveryErrorEventArgs : EventArgs
    {
        public ConnectionRecoveryErrorEventArgs(Exception ex)
        {
            Exception = ex;
        }

        public Exception Exception { get; private set; }
    }
}
