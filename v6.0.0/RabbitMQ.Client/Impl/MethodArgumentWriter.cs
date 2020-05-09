using System;
using System.Collections;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    internal struct MethodArgumentWriter
    {
        private byte _bitAccumulator;
        private int _bitMask;
        private bool _needBitFlush;
        public int Offset { get; private set; }
        public Memory<byte> Memory { get; }

        public MethodArgumentWriter(Memory<byte> memory)
        {
            Memory = memory;
            _needBitFlush = false;
            _bitAccumulator = 0;
            _bitMask = 1;
            Offset = 0;
        }

        public void Flush()
        {
            BitFlush();
        }

        public void WriteBit(bool val)
        {
            if (_bitMask > 0x80)
            {
                BitFlush();
            }
            if (val)
            {
                // The cast below is safe, because the combination of
                // the test against 0x80 above, and the action of
                // BitFlush(), causes m_bitMask never to exceed 0x80
                // at the point the following statement executes.
                _bitAccumulator = (byte)(_bitAccumulator | (byte)_bitMask);
            }
            _bitMask <<= 1;
            _needBitFlush = true;
        }

        public void WriteContent(byte[] val)
        {
            throw new NotSupportedException("WriteContent should not be called");
        }

        public void WriteLong(uint val)
        {
            BitFlush();
            Offset += WireFormatting.WriteLong(Memory.Slice(Offset), val);
        }

        public void WriteLonglong(ulong val)
        {
            BitFlush();
            Offset += WireFormatting.WriteLonglong(Memory.Slice(Offset), val);
        }

        public void WriteLongstr(byte[] val)
        {
            BitFlush();
            Offset += WireFormatting.WriteLongstr(Memory.Slice(Offset), val);
        }

        public void WriteOctet(byte val)
        {
            BitFlush();
            Memory.Slice(Offset++).Span[0] = val;
        }

        public void WriteShort(ushort val)
        {
            BitFlush();
            Offset += WireFormatting.WriteShort(Memory.Slice(Offset), val);
        }

        public void WriteShortstr(string val)
        {
            BitFlush();
            Offset += WireFormatting.WriteShortstr(Memory.Slice(Offset), val);
        }

        public void WriteTable(IDictionary val)
        {
            BitFlush();
            Offset += WireFormatting.WriteTable(Memory.Slice(Offset), val);
        }

        public void WriteTable(IDictionary<string, object> val)
        {
            BitFlush();
            Offset += WireFormatting.WriteTable(Memory.Slice(Offset), val);
        }

        public void WriteTimestamp(AmqpTimestamp val)
        {
            BitFlush();
            Offset += WireFormatting.WriteTimestamp(Memory.Slice(Offset), val);
        }

        private void BitFlush()
        {
            if (_needBitFlush)
            {
                Memory.Slice(Offset++).Span[0] = _bitAccumulator;
                ResetBitAccumulator();
            }
        }

        private void ResetBitAccumulator()
        {
            _needBitFlush = false;
            _bitAccumulator = 0;
            _bitMask = 1;
        }

        // TODO: Consider using NotImplementedException (?)
        // This is a completely bizarre consequence of the way the
        // Message.Transfer method is marked up in the XML spec.
    }
}
