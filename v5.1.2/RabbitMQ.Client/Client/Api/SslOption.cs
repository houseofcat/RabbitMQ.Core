using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Represents a configurable SSL option, used in setting up an SSL connection.
    /// </summary>
    public class SslOption
    {
        private X509CertificateCollection _certificateCollection;

        /// <summary>
        /// Constructs an SslOption specifying both the server cannonical name and the client's certificate path.
        /// </summary>
        public SslOption(string serverName, string certificatePath = "", bool enabled = false)
        {
            Version = SslProtocols.Tls;
            AcceptablePolicyErrors = SslPolicyErrors.None;

            ServerName = serverName;
            CertPath = certificatePath;
            Enabled = enabled;

            CertificateValidationCallback = null;
            CertificateSelectionCallback = null;
        }

        /// <summary>
        /// Constructs an <see cref="SslOption"/> with no parameters set.
        /// </summary>
        public SslOption()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Retrieve or set the set of ssl policy errors that are deemed acceptable.
        /// </summary>
        public SslPolicyErrors AcceptablePolicyErrors { get; set; }

        /// <summary>
        /// Retrieve or set the path to client certificate.
        /// </summary>
        public string CertPassphrase { get; set; }

        /// <summary>
        /// Retrieve or set the path to client certificate.
        /// </summary>
        public string CertPath { get; set; }

        /// <summary>
        /// An optional client specified SSL certificate selection callback.  If this is not specified,
        /// the first valid certificate found will be used.
        /// </summary>
        public LocalCertificateSelectionCallback CertificateSelectionCallback { get; set; }

        /// <summary>
        /// An optional client specified SSL certificate validation callback.  If this is not specified,
        /// the default callback will be used in conjunction with the <see cref="AcceptablePolicyErrors"/> property to
        /// determine if the remote server certificate is valid.
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        /// <summary>
        /// Retrieve or set the X509CertificateCollection containing the client certificate.
        /// If no collection is set, the client will attempt to load one from the specified <see cref="CertPath"/>.
        /// </summary>
        public X509CertificateCollection Certs
        {
            get
            {
                if (_certificateCollection != null)
                {
                    return _certificateCollection;
                }
                if (string.IsNullOrEmpty(CertPath))
                {
                    return null;
                }
                var collection = new X509CertificateCollection
                {
                    new X509Certificate2(CertPath, CertPassphrase)
                };
                return collection;
            }
            set { _certificateCollection = value; }
        }

        /// <summary>
        /// Flag specifying if Ssl should indeed be used.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Retrieve or set server's Canonical Name.
        /// This MUST match the CN on the Certificate else the SSL connection will fail.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Retrieve or set the Ssl protocol version.
        /// </summary>
        public SslProtocols Version { get; set; }
    }
}
