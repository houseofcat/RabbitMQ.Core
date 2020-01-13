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

    public class Workflow<TIn, TOut>
    {
        private readonly List<(IDataflowBlock Block, bool IsAsync)> pipelineSteps = new List<(IDataflowBlock Block, bool IsAsync)>();
        public bool Ready { get; private set; }
        private readonly SemaphoreSlim pipeLock = new SemaphoreSlim(1, 1);

        public void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc)
        {
            if (pipelineSteps.Count == 0)
            {
                pipelineSteps.Add((new TransformBlock<TLocalIn, TLocalOut>(stepFunc), IsAsync: false));
            }
            else
            {
                var (Block, IsAsync) = pipelineSteps.Last();
                if (!IsAsync)
                {
                    var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunc);

                    if (Block is ISourceBlock<TLocalIn> targetBlock)
                    {
                        targetBlock.LinkTo(step, new DataflowLinkOptions());
                        pipelineSteps.Add((step, IsAsync: false));
                    }
                }
                else
                {
                    var step = new TransformBlock<Task<TLocalIn>, TLocalOut>(
                        async (input) =>
                        stepFunc(await input.ConfigureAwait(false)));

                    if (Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                    {
                        targetBlock.LinkTo(step, new DataflowLinkOptions());
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
                        await stepFunc(input).ConfigureAwait(false));

                pipelineSteps.Add((step, IsAsync: true));
            }
            else
            {
                var (Block, IsAsync) = pipelineSteps.Last();
                if (IsAsync)
                {
                    var step = new TransformBlock<Task<TLocalIn>, Task<TLocalOut>>(
                        async (input) =>
                        await stepFunc(await input.ConfigureAwait(false))
                        .ConfigureAwait(false));

                    if (Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                    {
                        targetBlock.LinkTo(step, new DataflowLinkOptions());
                        pipelineSteps.Add((step, IsAsync: true));
                    }
                }
                else
                {
                    var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(
                        async (input) =>
                        await stepFunc(input)
                        .ConfigureAwait(false));

                    if (Block is ISourceBlock<TLocalIn> targetBlock)
                    {
                        targetBlock.LinkTo(step, new DataflowLinkOptions());
                        pipelineSteps.Add((step, IsAsync: true));
                    }
                }
            }
        }

        public async Task FinalizeAsync(Action<TOut> callBack)
        {
            await pipeLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (callBack != null)
                {
                    var (Block, IsAsync) = pipelineSteps.Last();
                    if (IsAsync)
                    {
                        var callBackStep = new ActionBlock<Task<TOut>>(
                            async t =>
                            callBack(await t.ConfigureAwait(false)));

                        if (Block is ISourceBlock<Task<TOut>> targetBlock)
                        {
                            targetBlock.LinkTo(callBackStep);
                        }
                    }
                    else
                    {
                        var callBackStep = new ActionBlock<TOut>(t => callBack(t));

                        if (Block is ISourceBlock<TOut> targetBlock)
                        {
                            targetBlock.LinkTo(callBackStep);
                        }
                    }
                }

                Ready = true;
            }
            finally
            { pipeLock.Release(); }
        }

        public async Task<bool> BeginAsync(TIn input)
        {
            return !Ready || pipelineSteps.Count == 0
                ? false
                : pipelineSteps[0].Block is ITargetBlock<TIn> firstStep ? await firstStep.SendAsync(input).ConfigureAwait(false) : false;
        }
    }
}
