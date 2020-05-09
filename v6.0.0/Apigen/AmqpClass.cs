using System.Collections.Generic;
using System.Xml;

namespace RabbitMQ.Client.Apigen
{
    public class AmqpClass : AmqpEntity
    {
        public IList<AmqpMethod> m_Methods;
        public IList<AmqpField> m_Fields;

        public AmqpClass(XmlNode n)
            : base(n)
        {
            m_Methods = new List<AmqpMethod>();
            foreach (XmlNode m in n.SelectNodes("method"))
            {
                m_Methods.Add(new AmqpMethod(m));
            }
            m_Fields = new List<AmqpField>();
            foreach (XmlNode f in n.SelectNodes("field"))
            {
                m_Fields.Add(new AmqpField(f));
            }
        }

        public int Index
        {
            get
            {
                return GetInt("@index");
            }
        }

        public bool NeedsProperties
        {
            get
            {
                foreach (AmqpMethod m in m_Methods)
                {
                    if (m.HasContent)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public AmqpMethod MethodNamed(string name)
        {
            foreach (AmqpMethod m in m_Methods)
            {
                if (m.Name == name)
                {
                    return m;
                }
            }
            return null;
        }
    }
}
