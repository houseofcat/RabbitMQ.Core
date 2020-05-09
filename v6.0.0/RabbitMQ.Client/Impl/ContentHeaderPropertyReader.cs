using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    struct ContentHeaderPropertyReader
    {
        private ushort m_bitCount;
        private ushort m_flagWord;
        private int _memoryOffset;
        private readonly ReadOnlyMemory<byte> _memory;

        public ContentHeaderPropertyReader(ReadOnlyMemory<byte> memory)
        {
            _memory = memory;
            _memoryOffset = 0;
            m_flagWord = 1; // just the continuation bit
            m_bitCount = 15; // the correct position to force a m_flagWord read
        }

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
            m_flagWord = NetworkOrderDeserializer.ReadUInt16(_memory.Slice(_memoryOffset));
            _memoryOffset += 2;
            m_bitCount = 0;
        }

        public uint ReadLong()
        {
            uint result = NetworkOrderDeserializer.ReadUInt32(_memory.Slice(_memoryOffset));
            _memoryOffset += 4;
            return result;
        }

        public ulong ReadLonglong()
        {
            ulong result = NetworkOrderDeserializer.ReadUInt64(_memory.Slice(_memoryOffset));
            _memoryOffset += 8;
            return result;
        }

        public byte[] ReadLongstr()
        {
            byte[] result = WireFormatting.ReadLongstr(_memory.Slice(_memoryOffset));
            _memoryOffset += 4 + result.Length;
            return result;
        }

        public byte ReadOctet()
        {
            return _memory.Span[_memoryOffset++];
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
            ushort result = NetworkOrderDeserializer.ReadUInt16(_memory.Slice(_memoryOffset));
            _memoryOffset += 2;
            return result;
        }

        public string ReadShortstr()
        {
            string result = WireFormatting.ReadShortstr(_memory.Slice(_memoryOffset), out int bytesRead);
            _memoryOffset += bytesRead;
            return result;
        }

        /// <returns>A type of <seealso cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.</returns>
        public IDictionary<string, object> ReadTable()
        {
            IDictionary<string, object> result = WireFormatting.ReadTable(_memory.Slice(_memoryOffset), out int bytesRead);
            _memoryOffset += bytesRead;
            return result;
        }

        public AmqpTimestamp ReadTimestamp()
        {
            AmqpTimestamp result = WireFormatting.ReadTimestamp(_memory.Slice(_memoryOffset));
            _memoryOffset += 8;
            return result;
        }
    }
}
