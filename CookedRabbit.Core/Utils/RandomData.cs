using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CookedRabbit.Core.Utils
{
    /// <summary>
    /// Static class for generating filler (random) data for users and Tests.
    /// </summary>
    public static class RandomData
    {
        private static readonly Random Rand = new Random();
        private static readonly XorShift XorShift = new XorShift(true);

        /// <summary>
        /// Used for testing purposes of creating random data filled Envelopes.
        /// </summary>
        /// <param name="queueNames"></param>
        /// <param name="payloadCount"></param>
        /// <param name="payloadSizeInBytes"></param>
        public static List<Letter> CreateRandomLetters(List<string> queueNames, int payloadCount, int payloadSizeInBytes = 1000)
        {
            var random = new Random();
            var letters = new List<Letter>();
            var payloads = CreatePayloads(payloadCount, payloadSizeInBytes);

            var queueCount = queueNames.Count;
            for (int i = 0; i < payloads.Count; i++)
            {
                letters.Add(
                    new Letter
                    {
                        Envelope = new Envelope
                        {
                            RoutingKey = queueNames[random.Next(0, queueCount)]
                        },
                        Body = payloads[i]
                    });
            }

            return letters;
        }

        /// <summary>
        /// Create a list of byte[] (default random data of 1KB each).
        /// </summary>
        /// <param name="payloadCount">The number of byte[]s to create.</param>
        /// <param name="payloadSizeInBytes">The size of random bytes per byte[].</param>
        /// <returns>List of byte[] random bytes.</returns>
        public static List<byte[]> CreatePayloads(int payloadCount, int payloadSizeInBytes = 1000)
        {
            if (payloadSizeInBytes < 100) throw new ArgumentException($"Argument {nameof(payloadSizeInBytes)} can't be less than 100 bytes.");

            var byteList = new List<byte[]>();

            for (int i = 0; i < payloadCount; i++)
            {
                byteList.Add(XorShift.UnsafeGetRandomBytes(payloadSizeInBytes));
            }

            return byteList;
        }

        private const string AllowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!@#$%^&*()_-+=";

        /// <summary>
        /// Random asynchronous string generator.
        /// </summary>
        /// <param name="minLength"></param>
        /// <param name="maxLength"></param>
        public static async Task<string> RandomStringAsync(int minLength, int maxLength)
        {
            return await Task.Run(() =>
            {
                char[] chars = new char[maxLength];
                int setLength = AllowedChars.Length;

                int length = Rand.Next(minLength, maxLength + 1);

                for (int i = 0; i < length; ++i)
                {
                    chars[i] = AllowedChars[Rand.Next(setLength)];
                }

                return new string(chars, 0, length);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Random string generator.
        /// </summary>
        /// <param name="minLength"></param>
        /// <param name="maxLength"></param>
        public static string RandomString(int minLength, int maxLength)
        {
            char[] chars = new char[maxLength];
            int setLength = AllowedChars.Length;

            int length = Rand.Next(minLength, maxLength + 1);

            for (int i = 0; i < length; ++i)
            {
                chars[i] = AllowedChars[Rand.Next(setLength)];
            }

            return new string(chars, 0, length);
        }
    }

    public class XorShift
    {
        private Random Rand { get; set; } = new Random();

        private uint X { get; set; }
        private uint Y { get; set; }
        private uint Z { get; set; }
        private uint W { get; set; }

        private const int Mask = 0xFF;

        public XorShift()
        {
            X = 123456789;
            Y = 362436069;
            Z = 521288629;
            W = 88675123;
        }

        public XorShift(bool reseed)
        {
            if (reseed)
            {
                var buffer = new byte[sizeof(uint)];
                Rand.NextBytes(buffer);
                X = BitConverter.ToUInt32(buffer);

                buffer = new byte[sizeof(uint)];
                Rand.NextBytes(buffer);
                Y = BitConverter.ToUInt32(buffer);

                buffer = new byte[sizeof(uint)];
                Rand.NextBytes(buffer);
                Z = BitConverter.ToUInt32(buffer);

                buffer = new byte[sizeof(uint)];
                Rand.NextBytes(buffer);
                W = BitConverter.ToUInt32(buffer);
            }
        }

        public byte[] GetRandomBytes(int size)
        {
            var buffer = new byte[size];
            FillBuffer(buffer, 0, size);
            return buffer;
        }

        public byte[] UnsafeGetRandomBytes(int size)
        {
            var buffer = new byte[size];
            UnsafeFillBuffer(buffer, 0, size);
            return buffer;
        }

        public void FillBuffer(byte[] buffer)
        {
            uint offset = 0, offsetEnd = (uint)buffer.Length;
            while (offset < offsetEnd)
            {
                uint t = X ^ (X << 11);
                X = Y; Y = Z; Z = W;
                W = W ^ (W >> 19) ^ (t ^ (t >> 8));

                if (offset < offsetEnd)
                {
                    buffer[offset++] = (byte)(W & Mask);
                    buffer[offset++] = (byte)((W >> 8) & Mask);
                    buffer[offset++] = (byte)((W >> 16) & Mask);
                    buffer[offset++] = (byte)((W >> 24) & Mask);
                }
                else { break; }
            }
        }

        public void FillBuffer(byte[] buffer, int offset, int offsetEnd)
        {
            while (offset < offsetEnd)
            {
                uint t = X ^ (X << 11);
                X = Y; Y = Z; Z = W;
                W = W ^ (W >> 19) ^ (t ^ (t >> 8));

                if (offset < offsetEnd)
                {
                    buffer[offset++] = (byte)(W & Mask);
                    buffer[offset++] = (byte)((W >> 8) & Mask);
                    buffer[offset++] = (byte)((W >> 16) & Mask);
                    buffer[offset++] = (byte)((W >> 24) & Mask);
                }
                else { break; }
            }
        }

        public unsafe void UnsafeFillBuffer(byte[] buf)
        {
            uint x = X, y = Y, z = Z, w = W;
            fixed (byte* pbytes = buf)
            {
                uint* pbuf = (uint*)(pbytes + 0);
                uint* pend = (uint*)(pbytes + buf.Length);
                while (pbuf < pend)
                {
                    uint tx = x ^ (x << 11);
                    uint ty = y ^ (y << 11);
                    uint tz = z ^ (z << 11);
                    uint tw = w ^ (w << 11);
                    *(pbuf++) = x = w ^ (w >> 19) ^ (tx ^ (tx >> 8));
                    *(pbuf++) = y = x ^ (x >> 19) ^ (ty ^ (ty >> 8));
                    *(pbuf++) = z = y ^ (y >> 19) ^ (tz ^ (tz >> 8));
                    *(pbuf++) = w = z ^ (z >> 19) ^ (tw ^ (tw >> 8));
                }
            }
            X = x; Y = y; Z = z; W = w;
        }

        public unsafe void UnsafeFillBuffer(byte[] buf, int offset, int offsetEnd)
        {
            uint x = X, y = Y, z = Z, w = W;
            fixed (byte* pbytes = buf)
            {
                uint* pbuf = (uint*)(pbytes + offset);
                uint* pend = (uint*)(pbytes + offsetEnd);
                while (pbuf < pend)
                {
                    uint tx = x ^ (x << 11);
                    uint ty = y ^ (y << 11);
                    uint tz = z ^ (z << 11);
                    uint tw = w ^ (w << 11);
                    *(pbuf++) = x = w ^ (w >> 19) ^ (tx ^ (tx >> 8));
                    *(pbuf++) = y = x ^ (x >> 19) ^ (ty ^ (ty >> 8));
                    *(pbuf++) = z = y ^ (y >> 19) ^ (tz ^ (tz >> 8));
                    *(pbuf++) = w = z ^ (z >> 19) ^ (tw ^ (tw >> 8));
                }
            }
            X = x; Y = y; Z = z; W = w;
        }
    }
}
