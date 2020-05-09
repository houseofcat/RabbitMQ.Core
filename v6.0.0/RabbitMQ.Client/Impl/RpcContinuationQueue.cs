using System;
using System.Threading;

namespace RabbitMQ.Client.Impl
{
    ///<summary>Manages a queue of waiting AMQP RPC requests.</summary>
    ///<remarks>
    ///<para>
    /// Currently, pipelining of requests is forbidden by this
    /// implementation. The AMQP 0-8 and 0-9 specifications themselves
    /// forbid pipelining, but only by the skin of their teeth and
    /// under a somewhat generous reading.
    ///</para>
    ///</remarks>
    internal class RpcContinuationQueue
    {
        private class EmptyRpcContinuation : IRpcContinuation
        {
            public void HandleCommand(Command cmd)
            {
            }

            public void HandleModelShutdown(ShutdownEventArgs reason)
            {
            }
        }

        private static readonly EmptyRpcContinuation s_tmp = new EmptyRpcContinuation();
        private IRpcContinuation _outstandingRpc = s_tmp;

        ///<summary>Enqueue a continuation, marking a pending RPC.</summary>
        ///<remarks>
        ///<para>
        /// Continuations are retrieved in FIFO order by calling Next().
        ///</para>
        ///<para>
        /// In the current implementation, only one continuation can
        /// be queued up at once. Calls to Enqueue() when a
        /// continuation is already enqueued will result in
        /// NotSupportedException being thrown.
        ///</para>
        ///</remarks>
        public void Enqueue(IRpcContinuation k)
        {
            IRpcContinuation result = Interlocked.CompareExchange(ref _outstandingRpc, k, s_tmp);
            if (!(result is EmptyRpcContinuation))
            {
                throw new NotSupportedException("Pipelining of requests forbidden");
            }
        }

        ///<summary>Interrupt all waiting continuations.</summary>
        ///<remarks>
        ///<para>
        /// There's just the one potential waiter in the current
        /// implementation.
        ///</para>
        ///</remarks>
        public void HandleModelShutdown(ShutdownEventArgs reason)
        {
            Next().HandleModelShutdown(reason);
        }

        ///<summary>Retrieve the next waiting continuation.</summary>
        ///<remarks>
        ///<para>
        /// It is an error to call this method when there are no
        /// waiting continuations. In the current implementation, if
        /// this happens, null will be returned (which will usually
        /// result in an immediate NullPointerException in the
        /// caller). Correct code will always arrange for a
        /// continuation to have been Enqueue()d before calling this
        /// method.
        ///</para>
        ///</remarks>
        public IRpcContinuation Next()
        {
            return Interlocked.Exchange(ref _outstandingRpc, s_tmp);
        }
    }
}
