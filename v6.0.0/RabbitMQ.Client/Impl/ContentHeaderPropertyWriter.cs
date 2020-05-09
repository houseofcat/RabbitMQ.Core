using RabbitMQ.Util;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    internal struct ContentHeaderPropertyWriter
    {
        private int _bitCount;
        private ushort _flagWord;
        public int Offset { get; private set; }
        public Memory<byte> Memory { get; }

        public ContentHeaderPropertyWriter(Memory<byte> memory)
        {
            Memory = memory;
            _flagWord = 0;
            _bitCount = 0;
            Offset = 0;
        }

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
            Offset += WireFormatting.WriteLong(Memory.Slice(Offset), val);
        }

        public void WriteLonglong(ulong val)
        {
            Offset += WireFormatting.WriteLonglong(Memory.Slice(Offset), val);
        }

        public void WriteLongstr(byte[] val)
        {
            Offset += WireFormatting.WriteLongstr(Memory.Slice(Offset), val);
        }

        public void WriteOctet(byte val)
        {
            Memory.Slice(Offset++).Span[0] = val;
        }

        public void WritePresence(bool present)
        {
            if (_bitCount == 15)
            {
                EmitFlagWord(true);
            }

            if (present)
            {
                int bit = 15 - _bitCount;
                _flagWord = (ushort)(_flagWord | (1 << bit));
            }
            _bitCount++;
        }

        public void WriteShort(ushort val)
        {
            Offset += WireFormatting.WriteShort(Memory.Slice(Offset), val);
        }

        public void WriteShortstr(string val)
        {
            Offset += WireFormatting.WriteShortstr(Memory.Slice(Offset), val);
        }

        public void WriteTable(IDictionary<string, object> val)
        {
            Offset += WireFormatting.WriteTable(Memory.Slice(Offset), val);
        }

        public void WriteTimestamp(AmqpTimestamp val)
        {
            Offset += WireFormatting.WriteTimestamp(Memory.Slice(Offset), val);
        }

        private void EmitFlagWord(bool continuationBit)
        {
            NetworkOrderSerializer.WriteUInt16(Memory.Slice(Offset), (ushort)(continuationBit ? (_flagWord | 1) : _flagWord));
            Offset += 2;
            _flagWord = 0;
            _bitCount = 0;
        }
    }
}
