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

        void AddAsyncStep<TLocalIn, TLocalOut>(Func<TLocalIn, Task<TLocalOut>> stepFunc, int? localMaxDoP = null, int? bufferSizeOverride = null);
        void AddStep<TLocalIn, TLocalOut>(Func<TLocalIn, TLocalOut> stepFunc, int? localMaxDoP = null, int? bufferSizeOverride = null);
        Task<bool> AwaitCompletionAsync();
        Task FinalizeAsync(Action<TOut> callBack = null);
        Task FinalizeAsync(Func<TOut, Task> callBack);
        IReadOnlyCollection<Exception> GetPipelineFaults();
        bool PipelineHasFault();
        Task<bool> QueueForExecutionAsync(TIn input);
    }
}