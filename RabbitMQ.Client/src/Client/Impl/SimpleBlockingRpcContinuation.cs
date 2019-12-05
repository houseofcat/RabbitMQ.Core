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
            switch (result.Alternative)
            {
                case EitherAlternative.Left:
                    return (Command)result.Value;
                case EitherAlternative.Right:
                    throw new OperationInterruptedException((ShutdownEventArgs)result.Value);
                default:
                    string error = "Illegal EitherAlternative " + result.Alternative;
#if !(NETFX_CORE)
                    // Trace.Fail(error);
#else
                    MetroEventSource.Log.Error(error);
#endif
                    return null;
            }
        }

        public virtual Command GetReply(TimeSpan timeout)
        {
            var result = (Either)m_cell.GetValue(timeout);
            switch (result.Alternative)
            {
                case EitherAlternative.Left:
                    return (Command)result.Value;
                case EitherAlternative.Right:
                    throw new OperationInterruptedException((ShutdownEventArgs)result.Value);
                default:
                    ReportInvalidInvariant(result);
                    return null;
            }
        }

        private static void ReportInvalidInvariant(Either result)
        {
            string error = "Illegal EitherAlternative " + result.Alternative;
#if !(NETFX_CORE)
            //Trace.Fail(error);
#else
            MetroEventSource.Log.Error(error);
#endif
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
