using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CookedRabbit.Core.WorkEngines
{
    public interface IPipeline<TIn, TOut>
    {
        int? BufferSize { get; }
        int MaxDegreeOfParallelism { get; }
        bool Ready { get; }
        int StepCount { get; }

        void AddAsyncStep<TLocalIn, TLocalOut>(Func<TLocalIn, Task<TLocalOut>> stepFunc, int? localMaxDoP = null, int? bufferSizeOverride = null);
        void AddAsyncSteps<TLocalIn, TLocalOut>(int? localMaxDoP = null, int? bufferSizeOverride = null, params Func<TLocalIn, Task<TLocalOut>>[] stepFunctions);
        void AddAsyncSteps<TLocalIn, TLocalOut>(Func<TLocalIn, Task<TLocalOut>>[] stepFunctions, int? localMaxDoP = null, int? bufferSizeOverride = null);
        void AddAsyncSteps<TLocalIn, TLocalOut>(List<Func<TLocalIn, Task<TLocalOut>>> stepFunctions, int? localMaxDoP = null, int? bufferSizeOverride = null);
        void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc, int? localMaxDoP = null, int? bufferSizeOverride = null);
        void AddSteps<TLocalIn, TLocalOut>(int? localMaxDoP = null, int? bufferSizeOverride = null, params Func<TLocalIn, TLocalOut>[] stepFunctions);
        void AddSteps<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut>[] stepFunctions, int? localMaxDoP = null, int? bufferSizeOverride = null);
        void AddSteps<TLocalIn, TLocalOut>(List<Func<TLocalIn, TLocalOut>> stepFunctions, int? localMaxDoP = null, int? bufferSizeOverride = null);
        Task<bool> AwaitCompletionAsync();
        void Finalize(Action<TOut> finalizeStep = null);
        void Finalize(Func<TOut, Task> finalizeStep = null);
        IReadOnlyCollection<Exception> GetPipelineFaults();
        bool PipelineHasFault();
        Task<bool> QueueForExecutionAsync(TIn input);
    }
}