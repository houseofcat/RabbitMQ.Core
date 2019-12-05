using RabbitMQ.Util;
using System.Collections.Generic;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Internal support class for use in reading and
    /// writing information binary-compatible with QPid's "MapMessage" wire encoding.
    /// </summary>
    /// <exception cref="ProtocolViolationException"/>
    public static class MapWireFormatting
    {
        public static IDictionary<string, object> ReadMap(NetworkBinaryReader reader)
        {
            int entryCount = BytesWireFormatting.ReadInt32(reader);
            if (entryCount < 0)
            {
                string message = string.Format("Invalid (negative) entryCount: {0}", entryCount);
                throw new ProtocolViolationException(message);
            }

            IDictionary<string, object> table = new Dictionary<string, object>(entryCount);
            for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                string key = StreamWireFormatting.ReadUntypedString(reader);
                object value = StreamWireFormatting.ReadObject(reader);
                table[key] = value;
            }

            return table;
        }

        public static void WriteMap(NetworkBinaryWriter writer, IDictionary<string, object> table)
        {
            int entryCount = table.Count;
            BytesWireFormatting.WriteInt32(writer, entryCount);

            foreach (KeyValuePair<string, object> entry in table)
            {
                StreamWireFormatting.WriteUntypedString(writer, entry.Key);
                StreamWireFormatting.WriteObject(writer, entry.Value);
            }
        }
    }
}
