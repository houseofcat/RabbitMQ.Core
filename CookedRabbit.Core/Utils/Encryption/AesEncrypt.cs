using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.IO;

namespace CookedRabbit.Core.Utils
{
    public static class AesEncrypt
    {
        public static Random Random { get; } = new Random();
        public static int MacBitSize { get; } = 128;
        public static int NonceSize { get; } = 12;
        public static int KeySize { get; } = 32;

        public static byte[] Encrypt(Span<byte> data, byte[] key)
        {
            //User Error Checks
            if (key == null
                || key.Length != KeySize
                || data.Length == 0)
            { return null; }

            var nonce = new byte[NonceSize];
            Random.NextBytes(nonce);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key), MacBitSize, nonce));

            var cipherText = new byte[cipher.GetOutputSize(data.Length)];
            cipher.DoFinal(cipherText, cipher.ProcessBytes(data.ToArray(), 0, data.Length, cipherText, 0));

            using (var cs = new MemoryStream())
            {
                using (var bw = new BinaryWriter(cs))
                {
                    bw.Write(nonce);
                    bw.Write(cipherText);
                }
                return cs.ToArray();
            }
        }

        public static byte[] Decrypt(Span<byte> encryptedData, byte[] key)
        {
            //User Error Checks
            if (key == null
                || key.Length != KeySize
                || encryptedData.Length == 0)
            { return null; }

            using var cipherStream = new MemoryStream(encryptedData.ToArray());
            using var cipherReader = new BinaryReader(cipherStream);

            var nonce = cipherReader.ReadBytes(NonceSize);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), MacBitSize, nonce);

            cipher.Init(false, parameters);

            var cipherText = cipherReader.ReadBytes(encryptedData.Length - nonce.Length);
            var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

            try
            { cipher.DoFinal(plainText, cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0)); }
            catch (InvalidCipherTextException)
            { return null; }

            return plainText;
        }
    }
}
