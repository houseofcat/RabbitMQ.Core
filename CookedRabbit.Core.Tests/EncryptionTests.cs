using CookedRabbit.Core.Utils;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CookedRabbit.Core.Tests
{
    public class EncryptionTests
    {
        private readonly ITestOutputHelper _output;
        const string Passphrase = "SuperNintendoHadTheBestZelda";
        const string Salt = "SegaGenesisIsTheBestConsole";

        public EncryptionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ArgonHashTest()
        {
            var hashKey = await ArgonHash
                .GetHashKeyAsync(Passphrase, Salt, 32)
                .ConfigureAwait(false);

            Assert.True(hashKey.Length == 32);
        }

        [Fact]
        public async Task EncryptDecryptTest()
        {
            var data = new byte[] { 0xFF, 0x00, 0xAA, 0xFF, 0x00 };

            var hashKey = await ArgonHash
                .GetHashKeyAsync(Passphrase, Salt, 32)
                .ConfigureAwait(false);

            _output.WriteLine(Encoding.UTF8.GetString(hashKey));
            _output.WriteLine($"HashKey: {Encoding.UTF8.GetString(hashKey)}");

            var encryptedData = AesEncrypt.Encrypt(data.AsSpan(), hashKey);
            _output.WriteLine($"Encrypted: {Encoding.UTF8.GetString(encryptedData)}");

            var decryptedData = AesEncrypt.Decrypt(encryptedData.AsSpan(), hashKey);
            _output.WriteLine($"Data: {Encoding.UTF8.GetString(data)}");
            _output.WriteLine($"Decrypted: {Encoding.UTF8.GetString(decryptedData)}");

            Assert.Equal(data, decryptedData);
        }
    }
}
