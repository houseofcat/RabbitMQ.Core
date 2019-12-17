using CookedRabbit.Core.Utils;
using System;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class UtilsTests
    {
        private readonly ITestOutputHelper output;

        public UtilsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CreateRandomBytes()
        {
            var xorShift = new XorShift();

            byte[] bytes0 = new byte[1000];
            byte[] bytes1 = new byte[1000];
            byte[] bytes2 = new byte[1000];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);
            xorShift.FillBuffer(bytes2, 0, bytes2.Length);

            Assert.NotEqual(bytes0, bytes1);
            Assert.NotEqual(bytes0, bytes2);
            Assert.NotEqual(bytes1, bytes2);
        }

        [Fact]
        public void CreateHundredRandomBytes()
        {
            var xorShift = new XorShift(true);

            byte[] bytes0 = new byte[100];
            byte[] bytes1 = new byte[100];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }

        [Fact]
        public void CreateFiveHundredRandomBytes()
        {
            var xorShift = new XorShift(true);

            byte[] bytes0 = new byte[500];
            byte[] bytes1 = new byte[500];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }

        [Fact]
        public void CreateThousandRandomBytes()
        {
            var xorShift = new XorShift(true);

            byte[] bytes0 = new byte[1_000];
            byte[] bytes1 = new byte[1_000];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }

        [Fact]
        public void CreateTenThousandRandomBytes()
        {
            var xorShift = new XorShift(true);

            byte[] bytes0 = new byte[10_000];
            byte[] bytes1 = new byte[10_000];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }

        [Fact]
        public void CreateHundredThousandRandomBytes()
        {
            var xorShift = new XorShift(true);

            byte[] bytes0 = new byte[100_000];
            byte[] bytes1 = new byte[100_000];

            xorShift.FillBuffer(bytes1, 0, bytes1.Length);

            Assert.NotEqual(bytes0, bytes1);
        }
    }
}
