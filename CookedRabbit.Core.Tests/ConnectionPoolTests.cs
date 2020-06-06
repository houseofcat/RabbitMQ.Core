using CookedRabbit.Core.Pools;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class ConnectionPoolTests
    {
        private readonly ITestOutputHelper output;

        public ConnectionPoolTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CreateConnectionPoolWithLocalHost()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var connPool = new ConnectionPool(config);

            Assert.NotNull(connPool);
        }

        [Fact]
        public async Task InitializeConnectionPoolAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var connPool = new ConnectionPool(config);

            Assert.NotNull(connPool);

            await connPool
                .InitializeAsync()
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task UseConnectionPoolBeforeInitializationAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var connPool = new ConnectionPool(config);

            Assert
                .NotNull(connPool);

            await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await connPool.GetConnectionAsync().ConfigureAwait(false))
                .ConfigureAwait(false);

            await Assert
                .ThrowsAsync<InvalidOperationException>(async () => await connPool.ShutdownAsync().ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task OverLoopThroughConnectionPoolAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            config.PoolSettings.MaxConnections = 5;
            var successCount = 0;
            const int loopCount = 100_000;
            var connPool = new ConnectionPool(config);

            await connPool.InitializeAsync().ConfigureAwait(false);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < loopCount; i++)
            {
                var connection = await connPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                if (connection != null)
                {
                    successCount++;
                }
            }

            sw.Stop();
            output.WriteLine($"OverLoop Iteration Time: {sw.ElapsedMilliseconds} ms");

            Assert.True(successCount == loopCount);
        }
    }
}
