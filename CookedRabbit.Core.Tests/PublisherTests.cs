using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CookedRabbit.Core.Configs;
using CookedRabbit.Core.Pools;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class PublisherTests
    {
        private readonly ITestOutputHelper output;

        public PublisherTests(ITestOutputHelper output)
        {
            this.output = output;
        }
    }
}
