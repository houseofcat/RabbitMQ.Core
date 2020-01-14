using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    // Great lessons/template found here.
    // https://michaelscodingspot.com/pipeline-implementations-csharp-3/

    public class Pipeline<TIn, TOut>
    {
        private readonly List<(IDataflowBlock Block, bool IsAsync)> pipelineSteps = new List<(IDataflowBlock Block, bool IsAsync)>();
        public bool Ready { get; private set; }
        private readonly SemaphoreSlim pipeLock = new SemaphoreSlim(1, 1);
        private ExecutionDataflowBlockOptions ExecuteStepOptions { get; }
        private DataflowLinkOptions LinkStepOptions { get; }
        public int MaxDegreeOfParallelism { get; }

        public Pipeline(int maxDegreeOfParallelism)
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            LinkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            ExecuteStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
            };
        }

        public void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
        {
            if (pipelineSteps.Count == 0)
            {
                pipelineSteps.Add((new TransformBlock<TLocalIn, TLocalOut>(stepFunc, ExecuteStepOptions), IsAsync: false));
            }
            else
            {
                var (Block, IsAsync) = pipelineSteps.Last();
                if (!IsAsync)
                {
                    var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunc, ExecuteStepOptions);

                    if (Block is ISourceBlock<TLocalIn> targetBlock)
                    {
                        targetBlock.LinkTo(step, LinkStepOptions);
                        pipelineSteps.Add((step, IsAsync: false));
                    }
                }
                else
                {
                    var step = new TransformBlock<Task<TLocalIn>, TLocalOut>(
                        async (input) =>
                        stepFunc(await input.ConfigureAwait(false)),
                        ExecuteStepOptions);

                    if (Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                    {
                        targetBlock.LinkTo(step, LinkStepOptions);
                        pipelineSteps.Add((step, IsAsync: false));
                    }
                }
            }
        }

        public void AddAsyncStep<TLocalIn, TLocalOut>(Func<TLocalIn, Task<TLocalOut>> stepFunc)
        {
            if (pipelineSteps.Count == 0)
            {
                var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(
                    async (input) =>
                    await stepFunc(input).ConfigureAwait(false),
                    ExecuteStepOptions);

                pipelineSteps.Add((step, IsAsync: true));
            }
            else
            {
                var (Block, IsAsync) = pipelineSteps.Last();
                if (IsAsync)
                {
                    var step = new TransformBlock<Task<TLocalIn>, Task<TLocalOut>>(
                        async (input) =>
                        await stepFunc(await input.ConfigureAwait(false)).ConfigureAwait(false),
                        ExecuteStepOptions);

                    if (Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                    {
                        targetBlock.LinkTo(step, LinkStepOptions);
                        pipelineSteps.Add((step, IsAsync: true));
                    }
                }
                else
                {
                    var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(
                        async (input) =>
                        await stepFunc(input).ConfigureAwait(false),
                        ExecuteStepOptions);

                    if (Block is ISourceBlock<TLocalIn> targetBlock)
                    {
                        targetBlock.LinkTo(step, LinkStepOptions);
                        pipelineSteps.Add((step, IsAsync: true));
                    }
                }
            }
        }

        public async Task FinalizeAsync(Action<TOut> callBack = null)
        {
            await pipeLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!Ready)
                {
                    if (callBack != null)
                    {
                        var (Block, IsAsync) = pipelineSteps.Last();
                        if (IsAsync)
                        {
                            var callBackStep = new ActionBlock<Task<TOut>>(
                                async t =>
                                callBack(await t.ConfigureAwait(false)),
                                ExecuteStepOptions);

                            if (Block is ISourceBlock<Task<TOut>> targetBlock)
                            {
                                targetBlock.LinkTo(callBackStep, LinkStepOptions);
                            }
                        }
                        else
                        {
                            var callBackStep = new ActionBlock<TOut>(t => callBack(t), ExecuteStepOptions);

                            if (Block is ISourceBlock<TOut> targetBlock)
                            {
                                targetBlock.LinkTo(callBackStep, LinkStepOptions);
                            }
                        }
                    }

                    Ready = true;
                }
            }
            finally
            { pipeLock.Release(); }
        }

        public async Task FinalizeAsync(Func<TOut, Task> callBack)
        {
            await pipeLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!Ready)
                {
                    if (callBack != null)
                    {
                        var (Block, IsAsync) = pipelineSteps.Last();
                        if (IsAsync)
                        {
                            var callBackStep = new ActionBlock<Task<TOut>>(
                                async t =>
                                await callBack(await t.ConfigureAwait(false))
                                .ConfigureAwait(false),
                                ExecuteStepOptions);

                            if (Block is ISourceBlock<Task<TOut>> targetBlock)
                            {
                                targetBlock.LinkTo(callBackStep, LinkStepOptions);
                            }
                        }
                        else
                        {
                            var callBackStep = new ActionBlock<TOut>(t => callBack(t), ExecuteStepOptions);

                            if (Block is ISourceBlock<TOut> targetBlock)
                            {
                                targetBlock.LinkTo(callBackStep, LinkStepOptions);
                            }
                        }
                    }

                    Ready = true;
                }
            }
            finally
            { pipeLock.Release(); }
        }

        public async Task<bool> QueueForExecutionAsync(TIn input)
        {
            if (!Ready || pipelineSteps.Count == 0)
            { return false; }

            if (pipelineSteps[0].Block is ITargetBlock<TIn> firstStep)
            {
                await firstStep.SendAsync(input).ConfigureAwait(false);
            }

            return false;
        }

        public async Task<bool> AwaitCompletionAsync()
        {
            if (!Ready || pipelineSteps.Count == 0)
            { return false; }

            if (pipelineSteps[0].Block is ITargetBlock<TIn> firstStep)
            {
                // Tell the pipeline its finished.
                firstStep.Complete();

                // Await the last step.
                if (pipelineSteps[pipelineSteps.Count - 1].Block is ITargetBlock<TIn> lastStep)
                {
                    await lastStep.Completion.ConfigureAwait(false);
                    return true;
                }
            }

            return false;
        }
    }
}