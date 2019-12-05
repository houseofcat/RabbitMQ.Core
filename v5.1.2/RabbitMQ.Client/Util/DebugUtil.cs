using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace RabbitMQ.Util
{
    ///<summary>Miscellaneous debugging and development utilities.</summary>
    ///<remarks>
    ///Not part of the public API.
    ///</remarks>
    public static class DebugUtil
    {
#if !(NETFX_CORE)
        ///<summary>Print a hex dump of the supplied bytes to stdout.</summary>
        public static void Dump(byte[] bytes)
        {
            Dump(bytes, Console.Out);
        }
#endif

        ///<summary>Print a hex dump of the supplied bytes to the supplied TextWriter.</summary>
        public static void Dump(byte[] bytes, TextWriter writer)
        {
            int rowlen = 16;

            for (int count = 0; count < bytes.Length; count += rowlen)
            {
                int thisRow = Math.Min(bytes.Length - count, rowlen);

                writer.Write("{0:X8}: ", count);
                for (int i = 0; i < thisRow; i++)
                {
                    writer.Write("{0:X2}", bytes[count + i]);
                }
                for (int i = 0; i < (rowlen - thisRow); i++)
                {
                    writer.Write("  ");
                }
                writer.Write("  ");
                for (int i = 0; i < thisRow; i++)
                {
                    if (bytes[count + i] >= 32 &&
                        bytes[count + i] < 128)
                    {
                        writer.Write((char)bytes[count + i]);
                    }
                    else
                    {
                        writer.Write('.');
                    }
                }
                writer.WriteLine();
            }
            if (bytes.Length % 16 != 0)
            {
                writer.WriteLine("{0:X8}: ", bytes.Length);
            }
        }

        ///<summary>Prints an indented key/value pair; used by DumpProperties()</summary>
        ///<remarks>Recurses into the value using DumpProperties().</remarks>
        public static void DumpKeyValue(string key, object value, TextWriter writer, int indent)
        {
            string prefix = new string(' ', indent + 2) + key + ": ";
            writer.Write(prefix);
            DumpProperties(value, writer, indent + 2);
        }

        ///<summary>Dump properties of objects to the supplied writer.</summary>
        public static void DumpProperties(object value, TextWriter writer, int indent)
        {
            if (value == null)
            {
                writer.WriteLine("(null)");
            }
            else if (value is string)
            {
                writer.WriteLine("\"" + ((string)value).Replace("\"", "\\\"") + "\"");
            }
            else if (value is byte[])
            {
                writer.WriteLine("byte[]");
                Dump((byte[])value, writer);
            }
            else if (value is ValueType)
            {
                writer.WriteLine(value);
            }
            else if (value is IDictionary)
            {
                Type t = value.GetType();
                writer.WriteLine(t.FullName);
                foreach (DictionaryEntry entry in ((IDictionary)value))
                {
                    DumpKeyValue(entry.Key.ToString(), entry.Value, writer, indent);
                }
            }
            else if (value is IEnumerable)
            {
                writer.WriteLine("IEnumerable");
                int index = 0;
                foreach (object v in ((IEnumerable)value))
                {
                    DumpKeyValue(index.ToString(), v, writer, indent);
                    index++;
                }
            }
            else
            {
                Type t = value.GetType();
                writer.WriteLine(t.FullName);
#if !(NETFX_CORE)
                foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Instance
                                                            | BindingFlags.Public
                                                            | BindingFlags.DeclaredOnly))
#else
                foreach (PropertyInfo pi in t.GetRuntimeProperties())
#endif
                {
                    if (pi.GetIndexParameters().Length == 0)
                    {
                        DumpKeyValue(pi.Name, pi.GetValue(value, new object[0]), writer, indent);
                    }
                }
            }
        }
    }
}
