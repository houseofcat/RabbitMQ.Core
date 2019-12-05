// This source code is dual-licensed under the Apache License, version
using System.Xml;

namespace RabbitMQ.Client.Apigen
{
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
