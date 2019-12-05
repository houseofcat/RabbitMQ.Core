using RabbitMQ.Util;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public class MethodArgumentReader
    {
        private int m_bit;
        private int m_bits;

        public MethodArgumentReader(NetworkBinaryReader reader)
        {
            BaseReader = reader;
            ClearBits();
        }

        public NetworkBinaryReader BaseReader { get; }

        public bool ReadBit()
        {
            if (m_bit > 0x80)
            {
                m_bits = BaseReader.ReadByte();
                m_bit = 0x01;
            }

            bool result = (m_bits & m_bit) != 0;
            m_bit <<= 1;
            return result;
        }

        public byte[] ReadContent()
        {
            throw new NotSupportedException("ReadContent should not be called");
        }

        public uint ReadLong()
        {
            ClearBits();
            return WireFormatting.ReadLong(BaseReader);
        }

        public ulong ReadLonglong()
        {
            ClearBits();
            return WireFormatting.ReadLonglong(BaseReader);
        }

        public byte[] ReadLongstr()
        {
            ClearBits();
            return WireFormatting.ReadLongstr(BaseReader);
        }

        public byte ReadOctet()
        {
            ClearBits();
            return WireFormatting.ReadOctet(BaseReader);
        }

        public ushort ReadShort()
        {
            ClearBits();
            return WireFormatting.ReadShort(BaseReader);
        }

        public string ReadShortstr()
        {
            ClearBits();
            return WireFormatting.ReadShortstr(BaseReader);
        }

        public IDictionary<string, object> ReadTable()
        {
            ClearBits();
            return WireFormatting.ReadTable(BaseReader);
        }

        public AmqpTimestamp ReadTimestamp()
        {
            ClearBits();
            return WireFormatting.ReadTimestamp(BaseReader);
        }

        private void ClearBits()
        {
            m_bits = 0;
            m_bit = 0x100;
        }

        // TODO: Consider using NotImplementedException (?)
        // This is a completely bizarre consequence of the way the
        // Message.Transfer method is marked up in the XML spec.
    }
}
