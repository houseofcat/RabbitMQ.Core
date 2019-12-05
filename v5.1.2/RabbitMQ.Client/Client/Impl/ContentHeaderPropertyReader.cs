using RabbitMQ.Util;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public class ContentHeaderPropertyReader
    {
        protected ushort m_bitCount;
        protected ushort m_flagWord;

        public ContentHeaderPropertyReader(NetworkBinaryReader reader)
        {
            BaseReader = reader;
            m_flagWord = 1; // just the continuation bit
            m_bitCount = 15; // the correct position to force a m_flagWord read
        }

        public NetworkBinaryReader BaseReader { get; }

        public bool ContinuationBitSet
        {
            get { return (m_flagWord & 1) != 0; }
        }

        public void FinishPresence()
        {
            if (ContinuationBitSet)
            {
                throw new MalformedFrameException("Unexpected continuation flag word");
            }
        }

        public bool ReadBit()
        {
            return ReadPresence();
        }

        public void ReadFlagWord()
        {
            if (!ContinuationBitSet)
            {
                throw new MalformedFrameException("Attempted to read flag word when none advertised");
            }
            m_flagWord = BaseReader.ReadUInt16();
            m_bitCount = 0;
        }

        public uint ReadLong()
        {
            return WireFormatting.ReadLong(BaseReader);
        }

        public ulong ReadLonglong()
        {
            return WireFormatting.ReadLonglong(BaseReader);
        }

        public byte[] ReadLongstr()
        {
            return WireFormatting.ReadLongstr(BaseReader);
        }

        public byte ReadOctet()
        {
            return WireFormatting.ReadOctet(BaseReader);
        }

        public bool ReadPresence()
        {
            if (m_bitCount == 15)
            {
                ReadFlagWord();
            }

            int bit = 15 - m_bitCount;
            bool result = (m_flagWord & (1 << bit)) != 0;
            m_bitCount++;
            return result;
        }

        public ushort ReadShort()
        {
            return WireFormatting.ReadShort(BaseReader);
        }

        public string ReadShortstr()
        {
            return WireFormatting.ReadShortstr(BaseReader);
        }

        public IDictionary<string, object> ReadTable()
        {
            return WireFormatting.ReadTable(BaseReader);
        }

        public AmqpTimestamp ReadTimestamp()
        {
            return WireFormatting.ReadTimestamp(BaseReader);
        }
    }
}
