using System;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary>
    /// Thrown when a session is destroyed during an RPC call to a
    /// broker. For example, if a TCP connection dropping causes the
    /// destruction of a session in the middle of a QueueDeclare
    /// operation, an OperationInterruptedException will be thrown to
    /// the caller of IModel.QueueDeclare.
    /// </summary>
    [Serializable]
    public class OperationInterruptedException
        // TODO: inherit from OperationCanceledException
        : RabbitMQClientException
    {
        ///<summary>Construct an OperationInterruptedException with
        ///the passed-in explanation, if any.</summary>
        public OperationInterruptedException(ShutdownEventArgs reason)
            : base(reason == null ? "The AMQP operation was interrupted" :
                $"The AMQP operation was interrupted: {reason}")
        {
            ShutdownReason = reason;
        }

        ///<summary>Construct an OperationInterruptedException with
        ///the passed-in explanation and prefix, if any.</summary>
        public OperationInterruptedException(ShutdownEventArgs reason, string prefix)
            : base(reason == null ? $"{prefix}: The AMQP operation was interrupted" :
                $"{prefix}: The AMQP operation was interrupted: {reason}")
        {
            ShutdownReason = reason;
        }

        protected OperationInterruptedException()
        {
        }

        protected OperationInterruptedException(string message) : base(message)
        {
        }

        protected OperationInterruptedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        ///<summary>Retrieves the explanation for the shutdown. May
        ///return null if no explanation is available.</summary>
        public ShutdownEventArgs ShutdownReason { get; protected set; }
    }
}
