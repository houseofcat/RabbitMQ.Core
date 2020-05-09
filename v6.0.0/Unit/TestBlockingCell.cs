using NUnit.Framework;
using RabbitMQ.Util;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    class TestBlockingCell : TimingFixture
    {
        public class DelayedSetter<T>
        {
            public BlockingCell<T> m_k;
            public TimeSpan m_delay;
            public T m_v;
            public void Run()
            {
                Thread.Sleep(m_delay);
                m_k.ContinueWithValue(m_v);
            }
        }

        public static void SetAfter<T>(TimeSpan delay, BlockingCell<T> k, T v)
        {
            var ds = new DelayedSetter<T>
            {
                m_k = k,
                m_delay = delay,
                m_v = v
            };
            new Thread(new ThreadStart(ds.Run)).Start();
        }

        public DateTime m_startTime;

        public void ResetTimer()
        {
            m_startTime = DateTime.Now;
        }

        public TimeSpan ElapsedMs()
        {
            return DateTime.Now - m_startTime;
        }

        [Test]
        public void TestSetBeforeGet()
        {
            var k = new BlockingCell<int>();
            k.ContinueWithValue(123);
            Assert.AreEqual(123, k.WaitForValue());
        }

        [Test]
        public void TestGetValueWhichDoesNotTimeOut()
        {
            var k = new BlockingCell<int>();
            k.ContinueWithValue(123);

            ResetTimer();
            int v = k.WaitForValue(TimingInterval);
            Assert.Greater(SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestGetValueWhichDoesTimeOut()
        {
            var k = new BlockingCell<int>();
            ResetTimer();
            Assert.Throws<TimeoutException>(() => k.WaitForValue(TimingInterval));
        }

        [Test]
        public void TestGetValueWhichDoesTimeOutWithTimeSpan()
        {
            var k = new BlockingCell<int>();
            ResetTimer();
            Assert.Throws<TimeoutException>(() => k.WaitForValue(TimingInterval));
        }

        [Test]
        public void TestGetValueWithTimeoutInfinite()
        {
            var k = new BlockingCell<int>();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            int v = k.WaitForValue(Timeout.InfiniteTimeSpan);
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateSucceeds()
        {
            var k = new BlockingCell<int>();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            int v = k.WaitForValue(TimingInterval_2X);
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateSucceedsWithTimeSpan()
        {
            var k = new BlockingCell<int>();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            int v = k.WaitForValue(TimingInterval_2X);
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateSucceedsWithInfiniteTimeoutTimeSpan()
        {
            var k = new BlockingCell<int>();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            TimeSpan infiniteTimeSpan = Timeout.InfiniteTimeSpan;
            int v = k.WaitForValue(infiniteTimeSpan);
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateFails()
        {
            var k = new BlockingCell<int>();
            SetAfter(TimingInterval_2X, k, 123);

            ResetTimer();
            Assert.Throws<TimeoutException>(() => k.WaitForValue(TimingInterval));
        }
    }
}
