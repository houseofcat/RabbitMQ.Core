using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RabbitMQ.Client
{
    public class ConsumerWorkService
    {
        private readonly ConcurrentDictionary<IModel, WorkPool> workPools = new ConcurrentDictionary<IModel, WorkPool>();

        public void AddWork(IModel model, Action fn)
        {
            // two step approach is taken, as TryGetValue does not aquire locks
            // if this fails, GetOrAdd is called, which takes a lock

            if (workPools.TryGetValue(model, out WorkPool workPool) == false)
            {
                var newWorkPool = new WorkPool(model);
                workPool = workPools.GetOrAdd(model, newWorkPool);

                // start if it's only the workpool that has been just created
                if (newWorkPool == workPool)
                {
                    newWorkPool.Start();
                }
            }

            workPool.Enqueue(fn);
        }

        public void StopWork(IModel model)
        {
            if (workPools.TryRemove(model, out WorkPool workPool))
            {
                workPool.Stop();
            }
        }

        public void StopWork()
        {
            foreach (var model in workPools.Keys)
            {
                StopWork(model);
            }
        }

        private class WorkPool
        {
            private readonly ConcurrentQueue<Action> actions;
            private readonly AutoResetEvent messageArrived;
            private readonly TimeSpan waitTime;
            private readonly CancellationTokenSource tokenSource;
            private readonly string name;

            public WorkPool(IModel model)
            {
                name = model.ToString();
                actions = new ConcurrentQueue<Action>();
                messageArrived = new AutoResetEvent(false);
                waitTime = TimeSpan.FromMilliseconds(100);
                tokenSource = new CancellationTokenSource();
            }

            public void Start()
            {
#if NETFX_CORE
                System.Threading.Tasks.Task.Factory.StartNew(Loop, System.Threading.Tasks.TaskCreationOptions.LongRunning);
#else
                var thread = new Thread(Loop)
                {
                    Name = "WorkPool-" + name,
                    IsBackground = true
                };
                thread.Start();
#endif
            }

            public void Enqueue(Action action)
            {
                actions.Enqueue(action);
                messageArrived.Set();
            }

            private void Loop()
            {
                while (tokenSource.IsCancellationRequested == false)
                {
                    while (actions.TryDequeue(out Action action))
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception)
                        {
                        }
                    }

                    messageArrived.WaitOne(waitTime);
                }
            }

            public void Stop()
            {
                tokenSource.Cancel();
            }
        }
    }
}
