using System.Diagnostics;
using System.Xml;

namespace RabbitMQ.Client.Apigen
{
    [DebuggerDisplay("{Domain,nq} {Name,nq}")]
    public class AmqpField : AmqpEntity
    {
        public AmqpField(XmlNode n) : base(n) { }

        public string Domain
        {
            get
            {
                string result = GetString("@domain", "");
                if (result.Equals(""))
                {
                    result = GetString("@type");
                }
                return result;
            }
        }
    }
}
