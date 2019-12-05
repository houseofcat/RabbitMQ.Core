using System.Collections.Generic;
using System.Xml;

namespace RabbitMQ.Client.Apigen
{
    public class AmqpMethod : AmqpEntity
    {
        public IList<AmqpField> m_Fields;
        public IList<string> m_ResponseMethods;

        public AmqpMethod(XmlNode n)
            : base(n)
        {
            m_Fields = new List<AmqpField>();
            foreach (XmlNode f in n.SelectNodes("field"))
            {
                m_Fields.Add(new AmqpField(f));
            }
            m_ResponseMethods = new List<string>();
            foreach (XmlNode r in n.SelectNodes("response"))
            {
                m_ResponseMethods.Add(Apigen.GetString(r, "@name"));
            }
        }

        public bool HasContent
        {
            get
            {
                return GetString("@content", null) != null;
            }
        }

        public bool IsSimpleRpcRequest
        {
            get
            {
                return m_ResponseMethods.Count == 1;
            }
        }

        public int Index
        {
            get
            {
                return GetInt("@index");
            }
        }
    }
}
