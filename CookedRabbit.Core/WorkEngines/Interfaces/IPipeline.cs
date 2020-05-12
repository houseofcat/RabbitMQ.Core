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

        /// <summary>
        /// Allows the chaining of steps from one type matched Pipeline to another.
        /// <para>The pipeline steps get added to the parent pipeline for logical consistency.</para>
        /// <para>TLocalIn refers to the output of the last step and input of the first step. They have to match, both in type and asynchrounous pattern.</para>
        /// <para>Because we don't have the original StepFunction, the blocks have to have matching inputs/outputs therefore async can attach to async, sync to sync.</para>
        /// </summary>
        /// <typeparam name="TLocalIn"></typeparam>
        /// <param name="pipeline"></param>
        void ChainPipeline<TLocalIn>(Pipeline<TIn, TOut> pipeline);

        void Finalize(Action<TOut> finalizeStep = null);
        void Finalize(Func<TOut, Task> finalizeStep = null);
        IReadOnlyCollection<Exception> GetPipelineFaults();
        bool PipelineHasFault();
        Task<bool> QueueForExecutionAsync(TIn input);
    }
}