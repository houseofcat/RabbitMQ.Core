using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CookedRabbit.Core.Configs;
using CookedRabbit.Core.Pools;

namespace Nuno.Rabbit.Benchmark
{
    [MemoryDiagnoser, ThreadingDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.NetCoreApp31)]
    public class ConnectionPoolBenchmark
    {
        public ConnectionPool ConnectionPool;

        [GlobalSetup]
        public async Task GlobalSetupAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            config.PoolSettings.MaxConnections = 25;

            ConnectionPool = new ConnectionPool(config);

            await ConnectionPool
                .InitializeAsync()
                .ConfigureAwait(false);
        }

        [GlobalCleanup]
        public async Task GlobalCleanupAsync()
        {
            await ConnectionPool
                .ShutdownAsync()
                .ConfigureAwait(false);
        }

        [Benchmark]
        [Arguments(5_000)]
        [Arguments(500_000)]
        public async Task OverLoopThroughConnectionPoolAsync(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var connection = await ConnectionPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);
            }
        }

        [Benchmark]
        [Arguments(1_000_000)]
        public async Task ConcurrentOverLoopThroughConnectionPoolAsync(int x)
        {
            var t1 = Task.Run(async () =>
            {
                for (int i = 0; i < x / 4; i++)
                {
                    var connection = await ConnectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);
                }
            });

            var t2 = Task.Run(async () =>
            {
                for (int i = 0; i < x / 4; i++)
                {
                    var connection = await ConnectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);
                }
            });

            var t3 = Task.Run(async () =>
            {
                for (int i = 0; i < x / 4; i++)
                {
                    var connection = await ConnectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);
                }
            });

            var t4 = Task.Run(async () =>
            {
                for (int i = 0; i < x / 4; i++)
                {
                    var connection = await ConnectionPool
                        .GetConnectionAsync()
                        .ConfigureAwait(false);
                }
            });

            await Task
                .WhenAll(new Task[] { t1, t2, t3, t4 })
                .ConfigureAwait(false);
        }
    }
}
