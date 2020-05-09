using RabbitMQ.Util;
using System;
using System.Text;

namespace RabbitMQ.Client.Impl
{
    abstract class ContentHeaderBase : IContentHeader
    {
        ///<summary>
        /// Retrieve the AMQP class ID of this content header.
        ///</summary>
        public abstract ushort ProtocolClassId { get; }

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
        internal ulong ReadFrom(ReadOnlyMemory<byte> memory)
        {
            // Skipping the first two bytes since they arent used (weight - not currently used)
            ulong bodySize = NetworkOrderDeserializer.ReadUInt64(memory.Slice(2));
            ContentHeaderPropertyReader reader = new ContentHeaderPropertyReader(memory.Slice(10));
            ReadPropertiesFrom(ref reader);
            return bodySize;
        }

        internal abstract void ReadPropertiesFrom(ref ContentHeaderPropertyReader reader);
        internal abstract void WritePropertiesTo(ref ContentHeaderPropertyWriter writer);

        private const ushort ZERO = 0;

        internal int WriteTo(Memory<byte> memory, ulong bodySize)
        {
            NetworkOrderSerializer.WriteUInt16(memory, ZERO); // Weight - not used
            NetworkOrderSerializer.WriteUInt64(memory.Slice(2), bodySize);

            ContentHeaderPropertyWriter writer = new ContentHeaderPropertyWriter(memory.Slice(10));
            WritePropertiesTo(ref writer);
            return 10 + writer.Offset;
        }
        public int GetRequiredBufferSize()
        {
            // The first 10 bytes are the Weight (2 bytes) + body size (8 bytes)
            return 10 + GetRequiredPayloadBufferSize();
        }

        public abstract int GetRequiredPayloadBufferSize();
    }
}
