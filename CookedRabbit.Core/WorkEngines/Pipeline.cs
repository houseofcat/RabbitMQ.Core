using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CookedRabbit.Core.WorkEngines
{
    public interface IPipeline<TIn, TOut>
    {
        bool Ready { get; }
        int StepCount { get; }
        List<PipelineStep> Steps { get; }

        void AddAsyncStep<TLocalIn, TLocalOut>(Func<TLocalIn, Task<TLocalOut>> stepFunc, int? localMaxDoP = null, bool? ensureOrdered = null, int? bufferSizeOverride = null);
        void AddAsyncSteps<TLocalIn, TLocalOut>(int? localMaxDoP = null, bool? ensureOrdered = null, int? bufferSizeOverride = null, params Func<TLocalIn, Task<TLocalOut>>[] stepFunctions);
        void AddAsyncSteps<TLocalIn, TLocalOut>(Func<TLocalIn, Task<TLocalOut>>[] stepFunctions, int? localMaxDoP = null, bool? ensureOrdered = null, int? bufferSizeOverride = null);
        void AddAsyncSteps<TLocalIn, TLocalOut>(List<Func<TLocalIn, Task<TLocalOut>>> stepFunctions, int? localMaxDoP = null, bool? ensureOrdered = null, int? bufferSizeOverride = null);
        void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc, int? localMaxDoP = null, bool? ensureOrdered = null, int? bufferSizeOverride = null);
        void AddSteps<TLocalIn, TLocalOut>(int? localMaxDoP = null, bool? ensureOrdered = null, int? bufferSizeOverride = null, params Func<TLocalIn, TLocalOut>[] stepFunctions);
        void AddSteps<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut>[] stepFunctions, int? localMaxDoP = null, bool? ensureOrdered = null, int? bufferSizeOverride = null);
        void AddSteps<TLocalIn, TLocalOut>(List<Func<TLocalIn, TLocalOut>> stepFunctions, int? localMaxDoP = null, bool? ensureOrdered = null, int? bufferSizeOverride = null);
        Task<bool> AwaitCompletionAsync();

        void Finalize(Action<TOut> finalizeStep);
        void Finalize(Func<TOut, Task> finalizeStep);
        Exception GetAnyPipelineStepsFault();
        Task<bool> QueueForExecutionAsync(TIn input);
    }

    // Great lesson/template found here.
    // https://michaelscodingspot.com/pipeline-implementations-csharp-3/

    public class Pipeline<TIn, TOut> : IPipeline<TIn, TOut>
    {
        private readonly ILogger<Pipeline<TIn, TOut>> _logger;
        private readonly SemaphoreSlim _pipeLock = new SemaphoreSlim(1, 1);
        private readonly ExecutionDataflowBlockOptions _executeStepOptions;
        private readonly DataflowLinkOptions _linkStepOptions;
        private readonly TimeSpan _healthCheckInterval;
        private readonly Task _healthCheckTask;
        private readonly string _pipelineName;

        public List<PipelineStep> Steps { get; private set; } = new List<PipelineStep>();
        public bool Ready { get; private set; }
        public int StepCount { get; private set; }

        public Pipeline(int maxDegreeOfParallelism, bool? ensureOrdered = null, int? bufferSize = null)
        {
            _logger = LogHelper.GetLogger<Pipeline<TIn, TOut>>();

            _linkStepOptions = new DataflowLinkOptions { PropagateCompletion = true };
            _executeStepOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
            };

            _executeStepOptions.EnsureOrdered = ensureOrdered ?? _executeStepOptions.EnsureOrdered;
            _executeStepOptions.BoundedCapacity = bufferSize ?? _executeStepOptions.BoundedCapacity;
        }

        public Pipeline(int maxDegreeOfParallelism, TimeSpan healthCheckInterval, string pipelineName, bool? ensureOrdered = null, int? bufferSize = null) : this(maxDegreeOfParallelism, ensureOrdered, bufferSize)
        {
            _pipelineName = pipelineName ?? Constants.DefaultPipelineName;
            _healthCheckInterval = healthCheckInterval;
            _healthCheckTask = Task.Run(() => SimplePipelineHealthTaskAsync());
        }

        public void AddAsyncStep<TLocalIn, TLocalOut>(
            Func<TLocalIn, Task<TLocalOut>> stepFunc,
            int? localMaxDoP = null,
            bool? ensureOrdered = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, ensureOrdered, bufferSizeOverride);
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
            bool? ensureOrdered = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, ensureOrdered, bufferSizeOverride);

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
            bool? ensureOrdered = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, ensureOrdered, bufferSizeOverride);

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
            bool? ensureOrdered = null,
            int? bufferSizeOverride = null,
            params Func<TLocalIn, Task<TLocalOut>>[] stepFunctions)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, ensureOrdered, bufferSizeOverride);

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
            bool? ensureOrdered = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, ensureOrdered, bufferSizeOverride);
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
            bool? ensureOrdered = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, ensureOrdered, bufferSizeOverride);

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
            bool? ensureOrdered = null,
            int? bufferSizeOverride = null)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, ensureOrdered, bufferSizeOverride);

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
            bool? ensureOrdered = null,
            int? bufferSizeOverride = null,
            params Func<TLocalIn, TLocalOut>[] stepFunctions)
        {
            if (Ready) throw new InvalidOperationException(ExceptionMessages.InvalidAddError);

            var options = GetExecuteStepOptions(localMaxDoP, ensureOrdered, bufferSizeOverride);

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

        public void Finalize(Action<TOut> finalizeStep)
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

        public void Finalize(Func<TOut, Task> finalizeStep)
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
                if (step.IsFaulted)
                {
                    return step.Block.Completion.Exception;
                }
            }

            return null;
        }

        private ExecutionDataflowBlockOptions GetExecuteStepOptions(int? maxDoPOverride, bool? ensureOrdered, int? bufferSizeOverride)
        {
            if (maxDoPOverride.HasValue || ensureOrdered.HasValue || bufferSizeOverride.HasValue)
            {
                var newOptions = new ExecutionDataflowBlockOptions();

                newOptions.EnsureOrdered = ensureOrdered ?? _executeStepOptions.EnsureOrdered;
                newOptions.MaxDegreeOfParallelism = maxDoPOverride ?? _executeStepOptions.MaxDegreeOfParallelism;
                newOptions.BoundedCapacity = bufferSizeOverride ?? _executeStepOptions.BoundedCapacity;

                return newOptions;
            }

            return _executeStepOptions;
        }

        private async Task SimplePipelineHealthTaskAsync()
        {
            await Task.Yield();

            while (true)
            {
                await Task.Delay(_healthCheckInterval).ConfigureAwait(false);

                var ex = GetAnyPipelineStepsFault();
                if (ex != null)
                { _logger.LogCritical(ex, LogMessages.Pipeline.Faulted, _pipelineName); }
                else  // No Steps are Faulted... Hooray!
                { _logger.LogDebug(LogMessages.Pipeline.Healthy, _pipelineName); }
            }
        }
    }
}