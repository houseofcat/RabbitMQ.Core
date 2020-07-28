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
        public void InitializeConnectionPoolAsync()
        {
            var config = new Config();
            config.FactorySettings.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            var connPool = new ConnectionPool(config);

            Assert.NotNull(connPool);
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

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < loopCount; i++)
            {
                var connHost = await connPool
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                if (connHost != null)
                {
                    successCount++;
                }

                await connPool
                    .ReturnConnectionAsync(connHost)
                    .ConfigureAwait(false);
            }

            sw.Stop();
            output.WriteLine($"OverLoop Iteration Time: {sw.ElapsedMilliseconds} ms");

            Assert.True(successCount == loopCount);
        }
    }
}
