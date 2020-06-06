using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CookedRabbit.Core.Pools;
using System;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser, ThreadingDiagnoser]
    [SimpleJob(runtimeMoniker: RuntimeMoniker.NetCoreApp31)]
    public class ChannelPoolBenchmark
    {
        public ConnectionPool ConnectionPool;
        public ChannelPool ChannelPool;

        [GlobalSetup]
        public async Task GlobalSetupAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            config.PoolSettings.MaxConnections = 5;
            config.PoolSettings.MaxChannels = 50;

            ChannelPool = new ChannelPool(config);
            ConnectionPool = new ConnectionPool(config);

            await ChannelPool
                .InitializeAsync()
                .ConfigureAwait(false);

            await ConnectionPool
                .InitializeAsync()
                .ConfigureAwait(false);
        }

        [GlobalCleanup]
        public async Task GlobalCleanupAsync()
        {
            await ChannelPool
                .ShutdownAsync()
                .ConfigureAwait(false);

            await ConnectionPool
                .ShutdownAsync()
                .ConfigureAwait(false);
        }

        [Benchmark(Baseline = true)]
        [Arguments(100)]
        [Arguments(500)]
        public void CreateConnectionsAndChannels(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var connection = ConnectionPool
                    .CreateConnection();

                var channel = connection
                    .CreateModel();

                channel.Close();
                connection.Close();
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        public async Task CreateChannelsWithConnectionFromConnectionPoolAsync(int x)
        {
            for (int i = 0; i < x; i++)
            {
                var connHost = await ConnectionPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                var channel = connHost.Connection.CreateModel();
                channel.Close();
            }
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(5_000)]
        [Arguments(500_000)]
        [Arguments(1_000_000)]
        public async Task GetChannelFromChannelPoolAsync(int x)
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
        [Arguments(100)]
        [Arguments(500)]
        [Arguments(5_000)]
        [Arguments(500_000)]
        [Arguments(1_000_000)]
        public async Task ConcurrentGetChannelFromChannelPoolAsync(int x)
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
