using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;
using System;

namespace RabbitMQ.Client.Impl
{
    public class SimpleBlockingRpcContinuation : IRpcContinuation
    {
        public readonly BlockingCell m_cell = new BlockingCell();

        public virtual Command GetReply()
        {
            var result = (Either)m_cell.Value;
            return result.Alternative switch
            {
                EitherAlternative.Left => (Command)result.Value,
                EitherAlternative.Right => throw new OperationInterruptedException((ShutdownEventArgs)result.Value),
                _ => null,
            };
        }

        public virtual Command GetReply(TimeSpan timeout)
        {
            var result = (Either)m_cell.GetValue(timeout);
            return result.Alternative switch
            {
                EitherAlternative.Left => (Command)result.Value,
                EitherAlternative.Right => throw new OperationInterruptedException((ShutdownEventArgs)result.Value),
                _ => null,
            };
        }

        public virtual void HandleCommand(Command cmd)
        {
            m_cell.Value = Either.Left(cmd);
        }

        public virtual void HandleModelShutdown(ShutdownEventArgs reason)
        {
            m_cell.Value = Either.Right(reason);
        }
    }
}
