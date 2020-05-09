using NUnit.Framework;
using PublicApiGenerator;
using System.Threading.Tasks;
using Verify;
using VerifyNUnit;

namespace RabbitMQ.Client.Unit
{
    [TestFixture]
    [Platform(Exclude = "Mono")]
    public class APIApproval
    {
        [Test]
        public Task Approve()
        {
            string publicApi = typeof(ConnectionFactory).Assembly.GeneratePublicApi(new ApiGeneratorOptions { ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" } });

            var settings = new VerifySettings();
            settings.DisableClipboard();
            settings.DisableDiff();

            return Verifier.Verify(publicApi, settings);
        }
    }
}

