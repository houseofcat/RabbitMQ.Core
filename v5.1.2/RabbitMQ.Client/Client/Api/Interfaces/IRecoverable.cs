using System;

namespace RabbitMQ.Client
{
    /// <summary>
    /// A marker interface for entities that are recoverable (currently connection or channel).
    /// </summary>
    public interface IRecoverable
    {
        event EventHandler<EventArgs> Recovery;
    }
}
