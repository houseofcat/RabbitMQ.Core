using BenchmarkDotNet.Running;

namespace CookedRabbit.Core.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            _ = BenchmarkRunner.Run<ConnectionPoolBenchmark>();
            _ = BenchmarkRunner.Run<ChannelPoolBenchmark>();
            _ = BenchmarkRunner.Run<UtilsBenchmark>();
        }
    }
}
