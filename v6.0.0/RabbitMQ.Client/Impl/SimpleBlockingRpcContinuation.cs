using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;
using System;

namespace RabbitMQ.Client.Impl
{
    internal class SimpleBlockingRpcContinuation : IRpcContinuation
    {
        public readonly BlockingCell<Either<Command, ShutdownEventArgs>> m_cell = new BlockingCell<Either<Command, ShutdownEventArgs>>();

        public virtual Command GetReply()
        {
            Either<Command, ShutdownEventArgs> result = m_cell.WaitForValue();
            switch (result.Alternative)
            {
                case EitherAlternative.Left:
                    return result.LeftValue;
                case EitherAlternative.Right:
                    throw new OperationInterruptedException(result.RightValue);
                default:
                    return null;
            }
        }

        public virtual Command GetReply(TimeSpan timeout)
        {
            Either<Command, ShutdownEventArgs> result = m_cell.WaitForValue(timeout);
            switch (result.Alternative)
            {
                case EitherAlternative.Left:
                    return result.LeftValue;
                case EitherAlternative.Right:
                    throw new OperationInterruptedException(result.RightValue);
                default:
                    return null;
            }
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
