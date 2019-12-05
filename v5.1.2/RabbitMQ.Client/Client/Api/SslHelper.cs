using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace RabbitMQ.Client
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
        /// Upgrade a Tcp stream to an Ssl stream using the SSL options provided.
        /// </summary>
        public static Stream TcpUpgrade(Stream tcpStream, SslOption sslOption)
        {
            var helper = new SslHelper(sslOption);

            var remoteCertValidator = sslOption.CertificateValidationCallback ?? helper.CertificateValidationCallback;
            var localCertSelector = sslOption.CertificateSelectionCallback ?? helper.CertificateSelectionCallback;

            var sslStream = new SslStream(tcpStream, false, remoteCertValidator, localCertSelector);

            sslStream.AuthenticateAsClientAsync(sslOption.ServerName, sslOption.Certs, sslOption.Version, false).GetAwaiter().GetResult();

            return sslStream;
        }

        private X509Certificate CertificateSelectionCallback(object sender, string targetHost,
            X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            if (acceptableIssuers?.Length > 0 && localCertificates?.Count > 0)
            {
                foreach (X509Certificate certificate in localCertificates)
                {
                    if (Array.IndexOf(acceptableIssuers, certificate.Issuer) != -1)
                    {
                        return certificate;
                    }
                }
            }
            if (localCertificates?.Count > 0)
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
