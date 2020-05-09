using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    public class ConsumerWorkService
    {
        private readonly ConcurrentDictionary<IModel, WorkPool> _workPools = new ConcurrentDictionary<IModel, WorkPool>();

        public void AddWork(IModel model, Action fn)
        {
            _workPools.GetOrAdd(model, StartNewWorkPool).Enqueue(fn);
        }

        private WorkPool StartNewWorkPool(IModel model)
        {
            var newWorkPool = new WorkPool();
            newWorkPool.Start();
            return newWorkPool;
        }

        public void StopWork()
        {
            foreach (IModel model in _workPools.Keys)
            {
                StopWork(model);
            }
        }

        public void StopWork(IModel model)
        {
            StopWorkAsync(model).GetAwaiter().GetResult();
        }

        public Task StopWorkAsync(IModel model)
        {
            if (_workPools.TryRemove(model, out WorkPool workPool))
            {
                return workPool.Stop();
            }

            return Task.CompletedTask;
        }

        private class WorkPool
        {
            private readonly ConcurrentQueue<Action> _actions;
            private readonly CancellationTokenSource _tokenSource;
            private readonly CancellationTokenRegistration _tokenRegistration;
            private TaskCompletionSource<bool> _syncSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private Task _worker;

            public WorkPool()
            {
                _actions = new ConcurrentQueue<Action>();
                _tokenSource = new CancellationTokenSource();
                _tokenRegistration = _tokenSource.Token.Register(() => _syncSource.TrySetCanceled());
            }

            public void Start()
            {
                _worker = Task.Run(Loop, CancellationToken.None);
            }

            public void Enqueue(Action action)
            {
                _actions.Enqueue(action);
                _syncSource.TrySetResult(true);
            }

            private async Task Loop()
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await _syncSource.Task.ConfigureAwait(false);
                        _syncSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                    catch (TaskCanceledException)
                    {
                        // Swallowing the task cancellation exception for the semaphore in case we are stopping.
                    }

                    while (_actions.TryDequeue(out Action action))
                    {
                        try
                        {
                            action();
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }

            public Task Stop()
            {
                _tokenSource.Cancel();
                _tokenRegistration.Dispose();
                return _worker;
            }
        }
    }
}
