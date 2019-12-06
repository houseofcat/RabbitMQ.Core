using NUnit.Framework;
using RabbitMQ.Util;
using System;
using System.IO;

namespace RabbitMQ.Client.Unit
{
    public class WireFormattingFixture
    {
        public static NetworkBinaryReader Reader(byte[] content)
        {
            return new NetworkBinaryReader(new MemoryStream(content));
        }

        public static NetworkBinaryWriter Writer()
        {
            return new NetworkBinaryWriter(new MemoryStream());
        }

        public static byte[] Contents(NetworkBinaryWriter w)
        {
            return ((MemoryStream)w.BaseStream).ToArray();
        }

        public void Check(NetworkBinaryWriter w, byte[] expected)
        {
            byte[] actual = Contents(w);
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
