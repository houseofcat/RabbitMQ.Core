using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    public class TestExchangeDeclare : IntegrationFixture
    {
        [Test]
        [Category("RequireSMP")]
        public void TestConcurrentExchangeDeclare()
        {
            string x = GenerateExchangeName();
            Random rnd = new Random();

            List<Thread> ts = new List<Thread>();
            System.NotSupportedException nse = null;
            for (int i = 0; i < 256; i++)
            {
                Thread t = new Thread(() =>
                        {
                            try
                            {
                                // sleep for a random amount of time to increase the chances
                                // of thread interleaving. MK.
                                Thread.Sleep(rnd.Next(5, 500));
                                Model.ExchangeDeclare(x, "fanout", false, false, null);
                            }
                            catch (System.NotSupportedException e)
                            {
                                nse = e;
                            }
                        });
                ts.Add(t);
                t.Start();
            }

            foreach (Thread t in ts)
            {
                t.Join();
            }

            Assert.IsNull(nse);
            Model.ExchangeDelete(x);
        }
    }
}
