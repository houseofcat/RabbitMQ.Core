using RabbitMQ.Util;
using System;
using System.Text;

namespace RabbitMQ.Client.Impl
{
    public abstract class ContentHeaderBase : IContentHeader
    {
        ///<summary>
        /// Retrieve the AMQP class ID of this content header.
        ///</summary>
        public abstract int ProtocolClassId { get; }

        ///<summary>
        /// Retrieve the AMQP class name of this content header.
        ///</summary>
        public abstract string ProtocolClassName { get; }

        public virtual object Clone()
        {
            throw new NotImplementedException();
        }

        public abstract void AppendPropertyDebugStringTo(StringBuilder stringBuilder);

        ///<summary>
        /// Fill this instance from the given byte buffer stream.
        ///</summary>
        public ulong ReadFrom(NetworkBinaryReader reader)
        {
            reader.ReadUInt16(); // weight - not currently used
            ulong bodySize = reader.ReadUInt64();
            ReadPropertiesFrom(new ContentHeaderPropertyReader(reader));
            return bodySize;
        }

        public abstract void ReadPropertiesFrom(ContentHeaderPropertyReader reader);
        public abstract void WritePropertiesTo(ContentHeaderPropertyWriter writer);

        public void WriteTo(NetworkBinaryWriter writer, ulong bodySize)
        {
            writer.Write((ushort)0); // weight - not currently used
            writer.Write(bodySize);
            WritePropertiesTo(new ContentHeaderPropertyWriter(writer));
        }
    }
}
