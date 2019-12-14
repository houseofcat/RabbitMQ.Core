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
    public class ChannelPoolBenchmark
    {
        public ChannelPool ChannelPool;

        [GlobalSetup]
        public async Task GlobalSetupAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            config.PoolSettings.MaxConnections = 5;
            config.PoolSettings.MaxChannels = 50;

            ChannelPool = new ChannelPool(config);

            await ChannelPool
                .InitializeAsync()
                .ConfigureAwait(false);
        }

        [GlobalCleanup]
        public async Task GlobalCleanupAsync()
        {
            await ChannelPool
                .ShutdownAsync()
                .ConfigureAwait(false);
        }

        [Benchmark]
        [Arguments(5_000)]
        [Arguments(500_000)]
        public async Task OverLoopThroughChannelPoolAsync(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var channel = await ChannelPool
                    .GetChannelAsync()
                    .ConfigureAwait(false);

                await ChannelPool
                    .ReturnChannelAsync(channel, false)
                    .ConfigureAwait(false);
            }
        }

        [Benchmark]
        [Arguments(1_000_000)]
        public async Task ConcurrentOverLoopThroughChannelPoolAsync(int x)
        {
            var t1 = Task.Run(async () =>
            {
                for (int i = 0; i < x; i++)
                {
                    var channel = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);

                    await ChannelPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            });

            var t2 = Task.Run(async () =>
            {
                for (int i = 0; i < x; i++)
                {
                    var channel = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);

                    await ChannelPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            });

            var t3 = Task.Run(async () =>
            {
                for (int i = 0; i < x; i++)
                {
                    var channel = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);

                    await ChannelPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            });

            var t4 = Task.Run(async () =>
            {
                for (int i = 0; i < x; i++)
                {
                    var channel = await ChannelPool
                        .GetChannelAsync()
                        .ConfigureAwait(false);

                    await ChannelPool
                        .ReturnChannelAsync(channel, false)
                        .ConfigureAwait(false);
                }
            });

            await Task
                .WhenAll(new Task[] { t1, t2, t3, t4 })
                .ConfigureAwait(false);
        }
    }
}
