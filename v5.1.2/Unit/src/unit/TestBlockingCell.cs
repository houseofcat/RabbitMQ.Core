using NUnit.Framework;
using RabbitMQ.Util;
using System;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestBlockingCell : TimingFixture
    {
        public class DelayedSetter
        {
            public BlockingCell m_k;
            public int m_delayMs;
            public object m_v;
            public void Run()
            {
                Thread.Sleep(m_delayMs);
                m_k.Value = m_v;
            }
        }

        public static void SetAfter(int delayMs, BlockingCell k, object v)
        {
            DelayedSetter ds = new DelayedSetter
            {
                m_k = k,
                m_delayMs = delayMs,
                m_v = v
            };
            new Thread(new ThreadStart(ds.Run)).Start();
        }

        public DateTime m_startTime;

        public void ResetTimer()
        {
            m_startTime = DateTime.Now;
        }

        public int ElapsedMs()
        {
            return (int)((DateTime.Now - m_startTime).TotalMilliseconds);
        }

        [Test]
        public void TestSetBeforeGet()
        {
            BlockingCell k = new BlockingCell
            {
                Value = 123
            };
            Assert.AreEqual(123, k.Value);
        }

        [Test]
        public void TestGetValueWhichDoesNotTimeOut()
        {
            BlockingCell k = new BlockingCell
            {
                Value = 123
            };

            ResetTimer();
            var v = k.GetValue(TimingInterval);
            Assert.Greater(SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestGetValueWhichDoesTimeOut()
        {
            BlockingCell k = new BlockingCell();
            ResetTimer();
            Assert.Throws<TimeoutException>(() => k.GetValue(TimingInterval));
        }

        [Test]
        public void TestGetValueWhichDoesTimeOutWithTimeSpan()
        {
            BlockingCell k = new BlockingCell();
            ResetTimer();
            Assert.Throws<TimeoutException>(() => k.GetValue(TimeSpan.FromMilliseconds(TimingInterval)));
        }

        [Test]
        public void TestGetValueWithTimeoutInfinite()
        {
            BlockingCell k = new BlockingCell();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            var v = k.GetValue(Timeout.Infinite);
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateSucceeds()
        {
            BlockingCell k = new BlockingCell();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            var v = k.GetValue(TimingInterval * 2);
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateSucceedsWithTimeSpan()
        {
            BlockingCell k = new BlockingCell();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            var v = k.GetValue(TimeSpan.FromMilliseconds(TimingInterval * 2));
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateSucceedsWithInfiniteTimeout()
        {
            BlockingCell k = new BlockingCell();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            var v = k.GetValue(Timeout.Infinite);
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateSucceedsWithInfiniteTimeoutTimeSpan()
        {
            BlockingCell k = new BlockingCell();
            SetAfter(TimingInterval, k, 123);

            ResetTimer();
            var infiniteTimeSpan = new TimeSpan(0, 0, 0, 0, Timeout.Infinite);
            var v = k.GetValue(infiniteTimeSpan);
            Assert.Less(TimingInterval - SafetyMargin, ElapsedMs());
            Assert.AreEqual(123, v);
        }

        [Test]
        public void TestBackgroundUpdateFails()
        {
            BlockingCell k = new BlockingCell();
            SetAfter(TimingInterval * 2, k, 123);

            ResetTimer();
            Assert.Throws<TimeoutException>(() => k.GetValue(TimingInterval));
        }
    }
}
