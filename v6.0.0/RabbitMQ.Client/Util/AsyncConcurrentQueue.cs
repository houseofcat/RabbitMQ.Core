using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Util
{
    /// <summary>
    /// A concurrent queue where Dequeue waits for something to be added if the queue is empty and Enqueue signals
    /// that something has been added. Similar in function to a BlockingCollection but with async/await
    /// support.
    /// </summary>
    /// <typeparam name="T">
    /// Queue element type
    /// </typeparam>
    public class AsyncConcurrentQueue<T>
    {
        private readonly ConcurrentQueue<T> _internalQueue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        /// <summary>
        /// Returns a Task that is completed when an object can be returned from the beginning of the queue.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<T> DequeueAsync(CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token).ConfigureAwait(false);

            if (!_internalQueue.TryDequeue(out var command))
            {
                throw new InvalidOperationException("public queue is empty despite receiving an enqueueing signal");
            }

            return command;
        }

        /// <summary>
        /// Add an object to the end of the queue.
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            _internalQueue.Enqueue(item);
            _semaphore.Release();
        }
    }
}
