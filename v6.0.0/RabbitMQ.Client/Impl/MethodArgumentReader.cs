using RabbitMQ.Util;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    internal struct MethodArgumentReader
    {
        private int? _bit;
        private int _bits;

        public MethodArgumentReader(ReadOnlyMemory<byte> memory)
        {
            _memory = memory;
            _memoryOffset = 0;
            _bits = 0;
            _bit = null;
        }

        private readonly ReadOnlyMemory<byte> _memory;
        private int _memoryOffset;

        public bool ReadBit()
        {
            if (!_bit.HasValue)
            {
                _bits = _memory.Span[_memoryOffset++];
                _bit = 0x01;
            }

            bool result = (_bits & _bit.Value) != 0;
            _bit <<= 1;
            return result;
        }

        public byte[] ReadContent()
        {
            throw new NotSupportedException("ReadContent should not be called");
        }

        public uint ReadLong()
        {
            ClearBits();
            uint result = NetworkOrderDeserializer.ReadUInt32(_memory.Slice(_memoryOffset));
            _memoryOffset += 4;
            return result;
        }

        public ulong ReadLonglong()
        {
            ClearBits();
            ulong result = NetworkOrderDeserializer.ReadUInt64(_memory.Slice(_memoryOffset));
            _memoryOffset += 8;
            return result;
        }

        public byte[] ReadLongstr()
        {
            ClearBits();
            byte[] result = WireFormatting.ReadLongstr(_memory.Slice(_memoryOffset));
            _memoryOffset += 4 + result.Length;
            return result;
        }

        public byte ReadOctet()
        {
            ClearBits();
            return _memory.Span[_memoryOffset++];
        }

        public ushort ReadShort()
        {
            ClearBits();
            ushort result = NetworkOrderDeserializer.ReadUInt16(_memory.Slice(_memoryOffset));
            _memoryOffset += 2;
            return result;
        }

        public string ReadShortstr()
        {
            ClearBits();
            string result = WireFormatting.ReadShortstr(_memory.Slice(_memoryOffset), out int bytesRead);
            _memoryOffset += bytesRead;
            return result;
        }

        public IDictionary<string, object> ReadTable()
        {
            ClearBits();
            IDictionary<string, object> result = WireFormatting.ReadTable(_memory.Slice(_memoryOffset), out int bytesRead);
            _memoryOffset += bytesRead;
            return result;
        }

        public AmqpTimestamp ReadTimestamp()
        {
            ClearBits();
            AmqpTimestamp result = WireFormatting.ReadTimestamp(_memory.Slice(_memoryOffset));
            _memoryOffset += 8;
            return result;
        }

        private void ClearBits()
        {
            _bits = 0;
            _bit = null;
        }

        // TODO: Consider using NotImplementedException (?)
        // This is a completely bizarre consequence of the way the
        // Message.Transfer method is marked up in the XML spec.
    }
}
