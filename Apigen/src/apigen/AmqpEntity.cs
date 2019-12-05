using System.Xml;

namespace RabbitMQ.Client.Apigen
{
    public class AmqpEntity
    {
        public XmlNode m_node;

        public AmqpEntity(XmlNode n)
        {
            m_node = n;
        }

        public string GetString(string path)
        {
            return Apigen.GetString(m_node, path);
        }

        public string GetString(string path, string d)
        {
            return Apigen.GetString(m_node, path, d);
        }

        public int GetInt(string path)
        {
            return Apigen.GetInt(m_node, path);
        }

        public string Name
        {
            get
            {
                return GetString("@name");
            }
        }

        public string DocumentationComment(string prefixSpaces)
        {
            return DocumentationComment(prefixSpaces, "doc");
        }

        public string DocumentationCommentVariant(string prefixSpaces, string tagname)
        {
            return DocumentationComment(prefixSpaces, "doc", tagname);
        }

        public string DocumentationComment(string prefixSpaces, string docXpath)
        {
            return DocumentationComment(prefixSpaces, docXpath, "summary");
        }

        public string DocumentationComment(string prefixSpaces, string docXpath, string tagname)
        {
            string docStr = GetString(docXpath, "").Trim();
            if (docStr.Length > 0)
            {
                return (prefixSpaces + "/// <" + tagname + ">\n" +
                        GetString(docXpath, "") + "\n</" + tagname + ">")
                    .Replace("\n", "\n" + prefixSpaces + "/// ");
            }
            else
            {
                return prefixSpaces + "// (no documentation)";
            }
        }
    }
}
