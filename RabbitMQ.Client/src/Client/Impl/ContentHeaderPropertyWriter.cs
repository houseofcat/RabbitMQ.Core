using RabbitMQ.Util;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public class ContentHeaderPropertyWriter
    {
        protected int m_bitCount;
        protected ushort m_flagWord;

        public ContentHeaderPropertyWriter(NetworkBinaryWriter writer)
        {
            BaseWriter = writer;
            m_flagWord = 0;
            m_bitCount = 0;
        }

        public NetworkBinaryWriter BaseWriter { get; }

        public void FinishPresence()
        {
            EmitFlagWord(false);
        }

        public void WriteBit(bool bit)
        {
            WritePresence(bit);
        }

        public void WriteLong(uint val)
        {
            WireFormatting.WriteLong(BaseWriter, val);
        }

        public void WriteLonglong(ulong val)
        {
            WireFormatting.WriteLonglong(BaseWriter, val);
        }

        public void WriteLongstr(byte[] val)
        {
            WireFormatting.WriteLongstr(BaseWriter, val);
        }

        public void WriteOctet(byte val)
        {
            WireFormatting.WriteOctet(BaseWriter, val);
        }

        public void WritePresence(bool present)
        {
            if (m_bitCount == 15)
            {
                EmitFlagWord(true);
            }

            if (present)
            {
                int bit = 15 - m_bitCount;
                m_flagWord = (ushort)(m_flagWord | (1 << bit));
            }
            m_bitCount++;
        }

        public void WriteShort(ushort val)
        {
            WireFormatting.WriteShort(BaseWriter, val);
        }

        public void WriteShortstr(string val)
        {
            WireFormatting.WriteShortstr(BaseWriter, val);
        }

        public void WriteTable(IDictionary<string, object> val)
        {
            WireFormatting.WriteTable(BaseWriter, val);
        }

        public void WriteTimestamp(AmqpTimestamp val)
        {
            WireFormatting.WriteTimestamp(BaseWriter, val);
        }

        private void EmitFlagWord(bool continuationBit)
        {
            BaseWriter.Write((ushort)(continuationBit ? (m_flagWord | 1) : m_flagWord));
            m_flagWord = 0;
            m_bitCount = 0;
        }
    }
}
