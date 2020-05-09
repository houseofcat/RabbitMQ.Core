using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    class ConnectionStartDetails
    {
        public byte[] m_locales;
        public byte[] m_mechanisms;
        public IDictionary<string, object> m_serverProperties;
        public byte m_versionMajor;
        public byte m_versionMinor;
    }
}
