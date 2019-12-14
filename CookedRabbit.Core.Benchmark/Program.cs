using BenchmarkDotNet.Running;

namespace Nuno.Rabbit.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            _ = BenchmarkRunner.Run<ConnectionPoolBenchmark>();
            _ = BenchmarkRunner.Run<ChannelPoolBenchmark>();
        }
    }
}
