using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Util;
using System.Buffers;

namespace RabbitMQ.Client.Impl
{
    internal enum AssemblyState
    {
        ExpectingMethod,
        ExpectingContentHeader,
        ExpectingContentBody,
        Complete
    }

    internal class CommandAssembler
    {
        private const int MaxArrayOfBytesSize = 2_147_483_591;

        public MethodBase m_method;
        public ContentHeaderBase m_header;
        public IMemoryOwner<byte> m_body;
        public ProtocolBase m_protocol;
        public int m_remainingBodyBytes;
        private int _offset;
        public AssemblyState m_state;

        public CommandAssembler(ProtocolBase protocol)
        {
            m_protocol = protocol;
            Reset();
        }

        public Command HandleFrame(InboundFrame f)
        {
            switch (m_state)
            {
                case AssemblyState.ExpectingMethod:
                    if (!f.IsMethod())
                    {
                        throw new UnexpectedFrameException(f);
                    }
                    m_method = m_protocol.DecodeMethodFrom(f.Payload);
                    m_state = m_method.HasContent ? AssemblyState.ExpectingContentHeader : AssemblyState.Complete;
                    return CompletedCommand();
                case AssemblyState.ExpectingContentHeader:
                    if (!f.IsHeader())
                    {
                        throw new UnexpectedFrameException(f);
                    }
                    m_header = m_protocol.DecodeContentHeaderFrom(NetworkOrderDeserializer.ReadUInt16(f.Payload));
                    ulong totalBodyBytes = m_header.ReadFrom(f.Payload.Slice(2));
                    if (totalBodyBytes > MaxArrayOfBytesSize)
                    {
                        throw new UnexpectedFrameException(f);
                    }

                    m_remainingBodyBytes = (int)totalBodyBytes;
                    m_body = MemoryPool<byte>.Shared.Rent(m_remainingBodyBytes);
                    UpdateContentBodyState();
                    return CompletedCommand();
                case AssemblyState.ExpectingContentBody:
                    if (!f.IsBody())
                    {
                        throw new UnexpectedFrameException(f);
                    }

                    if (f.Payload.Length > m_remainingBodyBytes)
                    {
                        throw new MalformedFrameException($"Overlong content body received - {m_remainingBodyBytes} bytes remaining, {f.Payload.Length} bytes received");
                    }

                    f.Payload.CopyTo(m_body.Memory.Slice(_offset));
                    m_remainingBodyBytes -= f.Payload.Length;
                    _offset += f.Payload.Length;
                    UpdateContentBodyState();
                    return CompletedCommand();
                case AssemblyState.Complete:
                default:
                    return null;
            }
        }

        private Command CompletedCommand()
        {
            if (m_state == AssemblyState.Complete)
            {
                Command result = new Command(m_method, m_header, m_body, _offset);
                Reset();
                return result;
            }
            else
            {
                return null;
            }
        }

        private void Reset()
        {
            m_state = AssemblyState.ExpectingMethod;
            m_method = null;
            m_header = null;
            m_body = null;
            _offset = 0;
            m_remainingBodyBytes = 0;
        }

        private void UpdateContentBodyState()
        {
            m_state = (m_remainingBodyBytes > 0)
                ? AssemblyState.ExpectingContentBody
                : AssemblyState.Complete;
        }
    }
}
