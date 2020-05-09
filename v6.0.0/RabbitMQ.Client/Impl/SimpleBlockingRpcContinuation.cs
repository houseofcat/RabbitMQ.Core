using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;
using System;

namespace RabbitMQ.Client.Impl
{
    public class SimpleBlockingRpcContinuation : IRpcContinuation
    {
        public readonly BlockingCell<Either<Command, ShutdownEventArgs>> m_cell = new BlockingCell<Either<Command, ShutdownEventArgs>>();

        public virtual Command GetReply()
        {
            Either<Command, ShutdownEventArgs> result = m_cell.WaitForValue();
            return result.Alternative switch
            {
                EitherAlternative.Left => result.LeftValue,
                EitherAlternative.Right => throw new OperationInterruptedException(result.RightValue),
                _ => null,
            };
        }

        public virtual Command GetReply(TimeSpan timeout)
        {
            Either<Command, ShutdownEventArgs> result = m_cell.WaitForValue(timeout);
            return result.Alternative switch
            {
                EitherAlternative.Left => result.LeftValue,
                EitherAlternative.Right => throw new OperationInterruptedException(result.RightValue),
                _ => null,
            };
        }

        public virtual void HandleCommand(Command cmd)
        {
            m_cell.ContinueWithValue(Either<Command, ShutdownEventArgs>.Left(cmd));
        }

        public virtual void HandleModelShutdown(ShutdownEventArgs reason)
        {
            m_cell.ContinueWithValue(Either<Command, ShutdownEventArgs>.Right(reason));
        }
    }
}
