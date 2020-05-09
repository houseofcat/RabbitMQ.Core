using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace RabbitMQ.Client.Impl
{
    /// <summary>
    /// Represents an <see cref="SslHelper"/> which does the actual heavy lifting to set up an SSL connection,
    ///  using the config options in an <see cref="SslOption"/> to make things cleaner.
    /// </summary>
    public sealed class SslHelper
    {
        private readonly SslOption _sslOption;

        private SslHelper(SslOption sslOption)
        {
            _sslOption = sslOption;
        }

        /// <summary>
        /// Upgrade a Tcp stream to an Ssl stream using the TLS options provided.
        /// </summary>
        public static Stream TcpUpgrade(Stream tcpStream, SslOption options)
        {
            var helper = new SslHelper(options);

            RemoteCertificateValidationCallback remoteCertValidator =
                options.CertificateValidationCallback ?? helper.CertificateValidationCallback;
            LocalCertificateSelectionCallback localCertSelector =
                options.CertificateSelectionCallback ?? helper.CertificateSelectionCallback;

            var sslStream = new SslStream(tcpStream, false, remoteCertValidator, localCertSelector);

            Action<SslOption> TryAuthenticating = (SslOption opts) =>
            {
                sslStream.AuthenticateAsClientAsync(opts.ServerName, opts.Certs, opts.Version,
                                                    opts.CheckCertificateRevocation).GetAwaiter().GetResult();
            };
            try
            {
                TryAuthenticating(options);
            }
            catch (ArgumentException e) when (e.ParamName == "sslProtocolType" && options.Version == SslProtocols.None)
            {
                // SslProtocols.None is dysfunctional in this environment, possibly due to TLS version restrictions
                // in the app context, system or .NET version-specific behavior. See rabbitmq/rabbitmq-dotnet-client#764
                // for background.
                options.UseFallbackTlsVersions();
                TryAuthenticating(options);
            }

            return sslStream;
        }

        private X509Certificate CertificateSelectionCallback(object sender, string targetHost,
            X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            if (acceptableIssuers != null && acceptableIssuers.Length > 0 &&
                localCertificates != null && localCertificates.Count > 0)
            {
                foreach (X509Certificate certificate in localCertificates)
                {
                    if (Array.IndexOf(acceptableIssuers, certificate.Issuer) != -1)
                    {
                        return certificate;
                    }
                }
            }
            if (localCertificates != null && localCertificates.Count > 0)
            {
                return localCertificates[0];
            }

            return null;
        }

        private bool CertificateValidationCallback(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return (sslPolicyErrors & ~_sslOption.AcceptablePolicyErrors) == SslPolicyErrors.None;
        }
    }
}
