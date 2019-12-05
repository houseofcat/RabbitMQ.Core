
#if !NETFX_CORE
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
#else
using Windows.Networking.Sockets;
#endif

namespace RabbitMQ.Client
{
    /// <summary>
    /// Represents a configurable SSL option, used in setting up an SSL connection.
    /// </summary>
    public class SslOption
    {
#if !NETFX_CORE
        private X509CertificateCollection _certificateCollection;
#endif

        /// <summary>
        /// Constructs an SslOption specifying both the server cannonical name and the client's certificate path.
        /// </summary>
        public SslOption(string serverName, string certificatePath = "", bool enabled = false)
        {
#if !NETFX_CORE
            Version = SslProtocols.Tls;
            AcceptablePolicyErrors = SslPolicyErrors.None;
#endif

            ServerName = serverName;
            CertPath = certificatePath;
            Enabled = enabled;

#if !NETFX_CORE
            CertificateValidationCallback = null;
            CertificateSelectionCallback = null;
#endif
        }

        /// <summary>
        /// Constructs an <see cref="SslOption"/> with no parameters set.
        /// </summary>
        public SslOption()
            : this(string.Empty)
        {
        }

#if !NETFX_CORE
        /// <summary>
        /// Retrieve or set the set of ssl policy errors that are deemed acceptable.
        /// </summary>
        public SslPolicyErrors AcceptablePolicyErrors { get; set; }
#endif

        /// <summary>
        /// Retrieve or set the path to client certificate.
        /// </summary>
        public string CertPassphrase { get; set; }

        /// <summary>
        /// Retrieve or set the path to client certificate.
        /// </summary>
        public string CertPath { get; set; }

#if !NETFX_CORE
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
#endif

        /// <summary>
        /// Flag specifying if Ssl should indeed be used.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Retrieve or set server's Canonical Name.
        /// This MUST match the CN on the Certificate else the SSL connection will fail.
        /// </summary>
        public string ServerName { get; set; }

#if !NETFX_CORE
        /// <summary>
        /// Retrieve or set the Ssl protocol version.
        /// </summary>
        public SslProtocols Version { get; set; }
#endif
    }
}
