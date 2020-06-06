using Microsoft.Extensions.Logging;
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
        private readonly ILogger<Pipeline<TIn, TOut>> _logger;
        private readonly SemaphoreSlim _pipeLock = new SemaphoreSlim(1, 1);
        private readonly ExecutionDataflowBlockOptions _executeStepOptions;
        private readonly DataflowLinkOptions _linkStepOptions;
        private int? _bufferSize;

        private TimeSpan _healthCheckInterval;
        private Task _healthCheckTask;
        private string _pipelineName;

        public List<PipelineStep> Steps { get; private set; } = new List<PipelineStep>();
        public int MaxDegreeOfParallelism { get; private set; }
        public bool Ready { get; private set; }
        public int StepCount { get; private set; }

        public Pipeline(int maxDegreeOfParallelism, int? bufferSize = null)
        {
            _logger = LogHelper.GetLogger<Pipeline<TIn, TOut>>();

            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            _bufferSize = bufferSize;
            _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _executeStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
            };

            _executeStepOptions.BoundedCapacity = bufferSize ?? _executeStepOptions.BoundedCapacity;
        }

        public Pipeline(int maxDegreeOfParallelism, TimeSpan healthCheckInterval, string pipelineName, int? bufferSize = null)
        {
            _logger = LogHelper.GetLogger<Pipeline<TIn, TOut>>();
            _healthCheckInterval = healthCheckInterval;
            _pipelineName = pipelineName ?? Constants.DefaultPipelineName;
            _healthCheckTask = Task.Run(() => SimplePipelineHealthTaskAsync());

            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            _bufferSize = bufferSize;
            _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _executeStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
            };


            _executeStepOptions.BoundedCapacity = bufferSize ?? _executeStepOptions.BoundedCapacity;
        }
        public Pipeline(int maxDegreeOfParallelism, Func<Task> healthCheck, int? bufferSize = null)
        {
            _logger = LogHelper.GetLogger<Pipeline<TIn, TOut>>();
            _healthCheckTask = Task.Run(() => healthCheck);

            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            _bufferSize = bufferSize;
            _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _executeStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
            };

            _executeStepOptions.BoundedCapacity = bufferSize ?? _executeStepOptions.BoundedCapacity;
        }

        public void AddAsyncStep<TLocalIn, TLocalOut>(
            Func<TLocalIn, Task<TLocalOut>> stepFunc,
            int? localMaxDoP = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

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
                        targetBlock.LinkTo(step, _linkStepOptions);
                        pipelineStep.Block = step;
                        Steps.Add(pipelineStep);
                    }
                    else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                }
                else
                {
                    var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunc, options);

                    if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                    {
                        targetBlock.LinkTo(step, _linkStepOptions);
                        pipelineStep.Block = step;
                        Steps.Add(pipelineStep);
                    }
                    else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                }
            }
        }

        public void AddAsyncSteps<TLocalIn, TLocalOut>(
            Func<TLocalIn, Task<TLocalOut>>[] stepFunctions,
            int? localMaxDoP = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, bufferSizeOverride);

            for (int i = 0; i < stepFunctions.Length; i++)
            {
                var pipelineStep = new PipelineStep
                {
                    IsAsync = true,
                    StepIndex = StepCount++,
                };

                if (Steps.Count == 0)
                {
                    pipelineStep.Block = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunctions[i], options);
                    Steps.Add(pipelineStep);
                }
                else
                {
                    var lastStep = Steps.Last();
                    if (lastStep.IsAsync)
                    {
                        var step = new TransformBlock<Task<TLocalIn>, Task<TLocalOut>>(
                            async (input) => stepFunctions[i](await input),
                            options);

                        if (lastStep.Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                    else
                    {
                        var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunctions[i], options);

                        if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                }
            }
        }

        public void AddAsyncSteps<TLocalIn, TLocalOut>(
            List<Func<TLocalIn, Task<TLocalOut>>> stepFunctions,
            int? localMaxDoP = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, bufferSizeOverride);

            for (int i = 0; i < stepFunctions.Count; i++)
            {
                var pipelineStep = new PipelineStep
                {
                    IsAsync = true,
                    StepIndex = StepCount++,
                };

                if (Steps.Count == 0)
                {
                    pipelineStep.Block = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunctions[i], options);
                    Steps.Add(pipelineStep);
                }
                else
                {
                    var lastStep = Steps.Last();
                    if (lastStep.IsAsync)
                    {
                        var step = new TransformBlock<Task<TLocalIn>, Task<TLocalOut>>(
                            async (input) => stepFunctions[i](await input),
                            options);

                        if (lastStep.Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                    else
                    {
                        var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunctions[i], options);

                        if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                }
            }
        }

        public void AddAsyncSteps<TLocalIn, TLocalOut>(
            int? localMaxDoP = null,
            int? bufferSizeOverride = null,
            params Func<TLocalIn, Task<TLocalOut>>[] stepFunctions)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, bufferSizeOverride);

            for (int i = 0; i < stepFunctions.Length; i++)
            {
                var pipelineStep = new PipelineStep
                {
                    IsAsync = true,
                    StepIndex = StepCount++,
                };

                if (Steps.Count == 0)
                {
                    pipelineStep.Block = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunctions[i], options);
                    Steps.Add(pipelineStep);
                }
                else
                {
                    var lastStep = Steps.Last();
                    if (lastStep.IsAsync)
                    {
                        var step = new TransformBlock<Task<TLocalIn>, Task<TLocalOut>>(
                            async (input) => stepFunctions[i](await input),
                            options);

                        if (lastStep.Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                    else
                    {
                        var step = new TransformBlock<TLocalIn, Task<TLocalOut>>(stepFunctions[i], options);

                        if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                }
            }
        }

        public void AddStep<TLocalIn, TLocalOut>(
            Func<TLocalIn, TLocalOut> stepFunc,
            int? localMaxDoP = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

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
                        targetBlock.LinkTo(step, _linkStepOptions);
                        pipelineStep.Block = step;
                        Steps.Add(pipelineStep);
                    }
                    else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                }
                else
                {
                    var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunc, options);
                    if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                    {
                        targetBlock.LinkTo(step, _linkStepOptions);
                        pipelineStep.Block = step;
                        Steps.Add(pipelineStep);
                    }
                    else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                }
            }
        }

        public void AddSteps<TLocalIn, TLocalOut>(
            Func<TLocalIn, TLocalOut>[] stepFunctions,
            int? localMaxDoP = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, bufferSizeOverride);

            for (int i = 0; i < stepFunctions.Length; i++)
            {
                var pipelineStep = new PipelineStep
                {
                    IsAsync = false,
                    StepIndex = StepCount++,
                };

                if (Steps.Count == 0)
                {
                    pipelineStep.Block = new TransformBlock<TLocalIn, TLocalOut>(stepFunctions[i], options);
                    Steps.Add(pipelineStep);
                }
                else
                {
                    var lastStep = Steps.Last();
                    if (lastStep.IsAsync)
                    {
                        var step = new TransformBlock<Task<TLocalIn>, TLocalOut>(
                            async (input) => stepFunctions[i](await input),
                            options);

                        if (lastStep.Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                    else
                    {
                        var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunctions[i], options);

                        if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                }
            }
        }

        public void AddSteps<TLocalIn, TLocalOut>(
            List<Func<TLocalIn, TLocalOut>> stepFunctions,
            int? localMaxDoP = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, bufferSizeOverride);

            for (int i = 0; i < stepFunctions.Count; i++)
            {
                var pipelineStep = new PipelineStep
                {
                    IsAsync = false,
                    StepIndex = StepCount++,
                };

                if (Steps.Count == 0)
                {
                    pipelineStep.Block = new TransformBlock<TLocalIn, TLocalOut>(stepFunctions[i], options);
                    Steps.Add(pipelineStep);
                }
                else
                {
                    var lastStep = Steps.Last();
                    if (lastStep.IsAsync)
                    {
                        var step = new TransformBlock<Task<TLocalIn>, TLocalOut>(
                            async (input) => stepFunctions[i](await input),
                            options);

                        if (lastStep.Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                    else
                    {
                        var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunctions[i], options);

                        if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                }
            }
        }

        public void AddSteps<TLocalIn, TLocalOut>(
            int? localMaxDoP = null,
            int? bufferSizeOverride = null,
            params Func<TLocalIn, TLocalOut>[] stepFunctions)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, bufferSizeOverride);

            for (int i = 0; i < stepFunctions.Length; i++)
            {
                var pipelineStep = new PipelineStep
                {
                    IsAsync = false,
                    StepIndex = StepCount++,
                };

                if (Steps.Count == 0)
                {
                    pipelineStep.Block = new TransformBlock<TLocalIn, TLocalOut>(stepFunctions[i], options);
                    Steps.Add(pipelineStep);
                }
                else
                {
                    var lastStep = Steps.Last();
                    if (lastStep.IsAsync)
                    {
                        var step = new TransformBlock<Task<TLocalIn>, TLocalOut>(
                            async (input) => stepFunctions[i](await input),
                            options);

                        if (lastStep.Block is ISourceBlock<Task<TLocalIn>> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                    else
                    {
                        var step = new TransformBlock<TLocalIn, TLocalOut>(stepFunctions[i], options);

                        if (lastStep.Block is ISourceBlock<TLocalIn> targetBlock)
                        {
                            targetBlock.LinkTo(step, _linkStepOptions);
                            pipelineStep.Block = step;
                            Steps.Add(pipelineStep);
                        }
                        else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                    }
                }
            }
        }

        /// <summary>
        /// Allows the chaining of steps from one type matched Pipeline to another.
        /// <para>The pipeline steps get added to the parent pipeline for logical consistency.</para>
        /// <para>TLocalIn refers to the output of the last step and input of the first step. They have to match, both in type and asynchrounous pattern.</para>
        /// <para>Because we don't have the original StepFunction, the blocks have to have matching inputs/outputs therefore async can attach to async, sync to sync.</para>
        /// </summary>
        /// <typeparam name="TLocalIn"></typeparam>
        /// <param name="pipeline"></param>
        public void ChainPipeline<TLocalIn>(Pipeline<TIn, TOut> pipeline)
        {
            if (pipeline.Ready || Ready) throw new InvalidOperationException(ExceptionMessages.ChainingImpossible);
            if (Steps.Count == 0 || pipeline.Steps.Count == 0) throw new InvalidOperationException(ExceptionMessages.NothingToChain);

            var lastStepThisPipeline = Steps.Last();
            var firstStepNewPipeline = pipeline.Steps.First();

            if (lastStepThisPipeline.IsAsync
                && firstStepNewPipeline.IsAsync
                && lastStepThisPipeline.Block is ISourceBlock<Task<TLocalIn>> asyncLastBlock
                && firstStepNewPipeline.Block is ITargetBlock<Task<TLocalIn>> asyncFirstBlock)
            {
                asyncLastBlock.LinkTo(asyncFirstBlock, _linkStepOptions);
            }
            else if (lastStepThisPipeline.Block is ISourceBlock<TLocalIn> lastBlock
                && firstStepNewPipeline.Block is ITargetBlock<TLocalIn> firstBlock)
            {
                lastBlock.LinkTo(firstBlock, _linkStepOptions);
            }
            else
            { throw new InvalidOperationException(ExceptionMessages.ChainingNotMatched); }

            foreach (var step in pipeline.Steps)
            {
                Steps.Add(step);
            }
        }

        public void Finalize(Action<TOut> finalizeStep = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.AlreadyFinalized);
            if (Steps.Count == 0) throw new InvalidOperationException(ExceptionMessages.CantFinalize);

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
                        async input => finalizeStep(await input),
                        _executeStepOptions);

                    if (lastStep.Block is ISourceBlock<Task<TOut>> targetBlock)
                    {
                        pipelineStep.Block = step;
                        targetBlock.LinkTo(step, _linkStepOptions);
                        Steps.Add(pipelineStep);
                    }
                    else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                }
                else
                {
                    var step = new ActionBlock<TOut>(
                        finalizeStep,
                        _executeStepOptions);

                    if (lastStep.Block is ISourceBlock<TOut> targetBlock)
                    {
                        pipelineStep.Block = step;
                        targetBlock.LinkTo(step, _linkStepOptions);
                        Steps.Add(pipelineStep);
                    }
                    else { throw new InvalidOperationException(ExceptionMessages.InvalidStepFound); }
                }
            }
            else
            { Steps.Last().IsLastStep = true; }

            Ready = true;
        }

        public void Finalize(Func<TOut, Task> finalizeStep = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.AlreadyFinalized);
            if (Steps.Count == 0) throw new InvalidOperationException(ExceptionMessages.CantFinalize);

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
                        _executeStepOptions);

                    if (lastStep.Block is ISourceBlock<Task<TOut>> targetBlock)
                    {
                        pipelineStep.Block = step;
                        targetBlock.LinkTo(step, _linkStepOptions);
                        Steps.Add(pipelineStep);
                    }
                }
                else
                {
                    var step = new ActionBlock<TOut>(t => finalizeStep(t), _executeStepOptions);
                    if (lastStep.Block is ISourceBlock<TOut> targetBlock)
                    {
                        pipelineStep.Block = step;
                        targetBlock.LinkTo(step, _linkStepOptions);
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
            if (!Ready) throw new InvalidOperationException(ExceptionMessages.NotFinalized);

            if (Steps[0].Block is ITargetBlock<TIn> firstStep)
            {

                _logger.LogTrace(LogMessages.Pipeline.Queued, _pipelineName);
                return await firstStep.SendAsync(input).ConfigureAwait(false);
            }

            return false;
        }

        public async Task<bool> AwaitCompletionAsync()
        {
            if (!Ready) throw new InvalidOperationException(ExceptionMessages.NotFinalized);

            if (Steps[0].Block is ITargetBlock<TIn> firstStep)
            {
                // Tell the pipeline its finished.
                firstStep.Complete();

                // Await the last step.
                if (Steps[Steps.Count - 1].Block is ITargetBlock<TIn> lastStep)
                {
                    _logger.LogTrace(LogMessages.Pipeline.AwaitsCompletion, _pipelineName);
                    await lastStep.Completion.ConfigureAwait(false);
                    return true;
                }
            }

            return false;
        }

        public Exception GetAnyPipelineStepsFault()
        {
            foreach (var step in Steps)
            {
                if (step.Block.Completion.IsFaulted)
                {
                    return step.Block.Completion.Exception;
                }
            }

            return null;
        }

        private ExecutionDataflowBlockOptions GetExecuteStepOptions(int? maxDoPOverride, int? bufferSizeOverride)
        {
            var options = _executeStepOptions;
            if (maxDoPOverride != null || bufferSizeOverride != null)
            {
                options = new ExecutionDataflowBlockOptions();

                options.MaxDegreeOfParallelism = maxDoPOverride ?? options.MaxDegreeOfParallelism;
                options.BoundedCapacity = bufferSizeOverride ?? options.BoundedCapacity;
            }
            return options;
        }

        private async Task SimplePipelineHealthTaskAsync()
        {
            await Task.Yield();

            while (true)
            {
                await Task.Delay(_healthCheckInterval);

                var ex = GetAnyPipelineStepsFault();
                if (ex != null) // No Steps are Faulted... Hooray!
                { _logger.LogCritical(ex, LogMessages.Pipeline.Faulted, _pipelineName); }
                else
                { _logger.LogDebug(LogMessages.Pipeline.Healthy, _pipelineName); }
            }
        }
    }
}