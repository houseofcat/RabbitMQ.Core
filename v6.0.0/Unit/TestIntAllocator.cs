using NUnit.Framework;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestIntAllocator
    {
        [Test]
        public void TestRandomAllocation()
        {
            int repeatCount = 10000;
            int range = 100;
            IList<int> allocated = new List<int>();
            IntAllocator intAllocator = new IntAllocator(0, range);
            Random rand = new Random();
            while (repeatCount-- > 0)
            {
                if (rand.Next(2) == 0)
                {
                    int a = intAllocator.Allocate();
                    if (a > -1)
                    {
                        Assert.False(allocated.Contains(a));
                        allocated.Add(a);
                    }
                }
                else if (allocated.Count > 0)
                {
                    int a = allocated[0];
                    intAllocator.Free(a);
                    allocated.RemoveAt(0);
                }
            }
        }

        [Test]
        public void TestAllocateAll()
        {
            int range = 100;
            IList<int> allocated = new List<int>();
            IntAllocator intAllocator = new IntAllocator(0, range);
            for (int i = 0; i <= range; i++)
            {
                int a = intAllocator.Allocate();
                Assert.AreNotEqual(-1, a);
                Assert.False(allocated.Contains(a));
                allocated.Add(a);
            }
        }
    }
}

