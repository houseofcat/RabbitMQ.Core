using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    // Great lesson/template found here.
    // https://michaelscodingspot.com/pipeline-implementations-csharp-3/

    public class Pipeline<TIn, TOut> : IPipeline<TIn, TOut>
    {
        private readonly List<PipelineStep> Steps = new List<PipelineStep>();
        public bool Ready { get; private set; }
        private readonly SemaphoreSlim pipeLock = new SemaphoreSlim(1, 1);
        private ExecutionDataflowBlockOptions ExecuteStepOptions { get; }
        private DataflowLinkOptions LinkStepOptions { get; }
        public int MaxDegreeOfParallelism { get; }
        public int? BufferSize { get; }
        public int StepCount { get; private set; }

        private const string NotFinalized = "Pipeline is not ready for receiving work as it has not been finalized yet.";
        private const string AlreadyFinalized = "Pipeline is already finalized and ready for use.";
        private const string CantFinalize = "Pipeline is can't finalize as no steps have been added.";
        private const string InvalidAddError = "Pipeline is already finalized and you can no longer add steps.";

        public Pipeline(int maxDegreeOfParallelism, int? bufferSize = null)
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            BufferSize = bufferSize;
            LinkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            ExecuteStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
            };

            ExecuteStepOptions.BoundedCapacity = bufferSize ?? ExecuteStepOptions.BoundedCapacity;
        }

        public void AddStep<TLocalIn, TLocalOut>(
            Func<TLocalIn, TLocalOut> stepFunc,
            int? localMaxDoP = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, bufferSizeOverride);
            var pipelineStep = new PipelineStep
            {
                IsAsync = false,
                StepIndex = StepCount++,
            };

            if (Steps.Count == 0)
            {
                pipelineStep.Block = new TransformBlock<TLocalIn, TLocalOut>(stepFunc, options);
                Steps.Add(pipelineStep);
            }
            else
            {
                var lastStep = Steps.Last();
                if (lastStep.IsAsync)
                {
                    var step = new TransformBlock<Task<TLocalIn>, TLocalOut>(
                        async (input) => stepFunc(await input),
                        options);

                    if (lastStep.Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                    {
                        targetBlock.LinkTo(step, LinkStepOptions);
                        pipelineStep.Block = step;
                        Steps.Add(pipelineStep);
                    }
                }
                else
                {
                    var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunc, options);
                    if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                    {
                        targetBlock.LinkTo(step, LinkStepOptions);
                        pipelineStep.Block = step;
                        Steps.Add(pipelineStep);
                    }
                }
            }
        }

        public void AddAsyncStep<TLocalIn, TLocalOut>(Func<TLocalIn, Task<TLocalOut>> stepFunc, int? localMaxDoP = null, int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, bufferSizeOverride);
            var pipelineStep = new PipelineStep
            {
                IsAsync = true,
                StepIndex = StepCount++,
            };

            if (Steps.Count == 0)
            {
                pipelineStep.Block = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunc, options);
                Steps.Add(pipelineStep);
            }
            else
            {
                var lastStep = Steps.Last();
                if (lastStep.IsAsync)
                {
                    var step = new TransformBlock<Task<TLocalIn>, Task<TLocalOut>>(
                        async (input) => stepFunc(await input),
                        options);

                    if (lastStep.Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                    {
                        targetBlock.LinkTo(step, LinkStepOptions);
                        pipelineStep.Block = step;
                        Steps.Add(pipelineStep);
                    }
                }
                else
                {
                    var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunc, options);

                    if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                    {
                        targetBlock.LinkTo(step, LinkStepOptions);
                        pipelineStep.Block = step;
                        Steps.Add(pipelineStep);
                    }
                }
            }
        }

        public void Finalize(Action<TOut> finalizeStep = null)
        {
            if (Ready) throw new InvalidOperationException(AlreadyFinalized);
            if (Steps.Count == 0) throw new InvalidOperationException(CantFinalize);

            if (finalizeStep != null)
            {
                var pipelineStep = new PipelineStep
                {
                    IsAsync = false,
                    StepIndex = StepCount++,
                    IsLastStep = true,
                };

                var lastStep = Steps.Last();
                if (lastStep.IsAsync)
                {
                    var step = new ActionBlock<Task<TOut>>(
                        async t => finalizeStep(await t),
                        ExecuteStepOptions);

                    if (lastStep.Block is ISourceBlock<Task<TOut>> targetBlock)
                    {
                        pipelineStep.Block = step;
                        targetBlock.LinkTo(step, LinkStepOptions);
                        Steps.Add(pipelineStep);
                    }
                }
                else
                {
                    var step = new ActionBlock<TOut>(
                        t => finalizeStep(t),
                        ExecuteStepOptions);

                    if (lastStep.Block is ISourceBlock<TOut> targetBlock)
                    {
                        pipelineStep.Block = step;
                        targetBlock.LinkTo(step, LinkStepOptions);
                        Steps.Add(pipelineStep);
                    }
                }
            }
            else
            {
                var lastStep = Steps.Last();
                lastStep.IsLastStep = true;
            }

            Ready = true;
        }

        public void Finalize(Func<TOut, Task> finalizeStep = null)
        {
            if (Ready) throw new InvalidOperationException(AlreadyFinalized);
            if (Steps.Count == 0) throw new InvalidOperationException(CantFinalize);

            if (finalizeStep != null)
            {
                var pipelineStep = new PipelineStep
                {
                    IsAsync = true,
                    StepIndex = StepCount++,
                    IsLastStep = true,
                };

                var lastStep = Steps.Last();
                if (lastStep.IsAsync)
                {
                    var step = new ActionBlock<Task<TOut>>(
                        async t => await finalizeStep(await t),
                        ExecuteStepOptions);

                    if (lastStep.Block is ISourceBlock<Task<TOut>> targetBlock)
                    {
                        pipelineStep.Block = step;
                        targetBlock.LinkTo(step, LinkStepOptions);
                        Steps.Add(pipelineStep);
                    }
                }
                else
                {
                    var step = new ActionBlock<TOut>(t => finalizeStep(t), ExecuteStepOptions);
                    if (lastStep.Block is ISourceBlock<TOut> targetBlock)
                    {
                        pipelineStep.Block = step;
                        targetBlock.LinkTo(step, LinkStepOptions);
                        Steps.Add(pipelineStep);
                    }
                }
            }
            else
            {
                var lastStep = Steps.Last();
                lastStep.IsLastStep = true;
            }

            Ready = true;
        }

        public async Task<bool> QueueForExecutionAsync(TIn input)
        {
            if (!Ready) throw new InvalidOperationException(NotFinalized);

            if (Steps[0].Block is ITargetBlock<TIn> firstStep)
            {
                await firstStep.SendAsync(input).ConfigureAwait(false);
            }

            return false;
        }

        public async Task<bool> AwaitCompletionAsync()
        {
            if (!Ready) throw new InvalidOperationException(NotFinalized);

            if (Steps[0].Block is ITargetBlock<TIn> firstStep)
            {
                // Tell the pipeline its finished.
                firstStep.Complete();

                // Await the last step.
                if (Steps[Steps.Count - 1].Block is ITargetBlock<TIn> lastStep)
                {
                    await lastStep.Completion.ConfigureAwait(false);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Let's you identify if any of your step Tasks are in a faulted state.
        /// </summary>
        public bool PipelineHasFault()
        {
            foreach (var step in Steps)
            {
                if (step.Block.Completion.IsFaulted)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Let's you get any InnerExceptions from any faulted tasks.
        /// </summary>
        public IReadOnlyCollection<Exception> GetPipelineFaults()
        {
            foreach (var step in Steps)
            {
                if (step.IsFaulted)
                {
                    return step.Block.Completion.Exception.InnerExceptions;
                }
            }

            return null;
        }

        private ExecutionDataflowBlockOptions GetExecuteStepOptions(int? maxDoPOverride, int? bufferSizeOverride)
        {
            var options = ExecuteStepOptions;
            if (maxDoPOverride != null || bufferSizeOverride != null)
            {
                options = new ExecutionDataflowBlockOptions();

                options.MaxDegreeOfParallelism = maxDoPOverride ?? options.MaxDegreeOfParallelism;
                options.BoundedCapacity = bufferSizeOverride ?? options.BoundedCapacity;
            }
            return options;
        }
    }
}