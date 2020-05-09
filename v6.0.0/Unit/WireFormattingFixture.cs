using NUnit.Framework;
using RabbitMQ.Util;
using System;

namespace RabbitMQ.Client.Unit
{
    class WireFormattingFixture
    {
        public void Check(byte[] actual, byte[] expected)
        {
            try
            {
                Assert.AreEqual(expected, actual);
            }
            catch
            {
                Console.WriteLine();
                Console.WriteLine("EXPECTED ==================================================");
                DebugUtil.Dump(expected, Console.Out);
                Console.WriteLine("ACTUAL ====================================================");
                DebugUtil.Dump(actual, Console.Out);
                Console.WriteLine("===========================================================");
                throw;
            }
        }
    }
}
