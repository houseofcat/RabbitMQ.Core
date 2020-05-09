using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing;
using RabbitMQ.Util;
using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;

namespace RabbitMQ.Client.Impl
{
    public class HeaderOutboundFrame : OutboundFrame
    {
        private readonly ContentHeaderBase _header;
        private readonly int _bodyLength;

        public HeaderOutboundFrame(int channel, ContentHeaderBase header, int bodyLength) : base(FrameType.FrameHeader, channel)
        {
            _header = header;
            _bodyLength = bodyLength;
        }

        public override int GetMinimumPayloadBufferSize()
        {
            // ProtocolClassId (2) + header (X bytes)
            return 2 + _header.GetRequiredBufferSize();
        }

        public override int WritePayload(Memory<byte> memory)
        {
            // write protocol class id (2 bytes)
            NetworkOrderSerializer.WriteUInt16(memory, _header.ProtocolClassId);
            // write header (X bytes)
            int bytesWritten = _header.WriteTo(memory.Slice(2), (ulong)_bodyLength);
            return 2 + bytesWritten;
        }
    }

    public class BodySegmentOutboundFrame : OutboundFrame
    {
        private readonly ReadOnlyMemory<byte> _body;

        public BodySegmentOutboundFrame(int channel, ReadOnlyMemory<byte> bodySegment) : base(FrameType.FrameBody, channel)
        {
            _body = bodySegment;
        }

        public override int GetMinimumPayloadBufferSize()
        {
            return _body.Length;
        }

        public override int WritePayload(Memory<byte> memory)
        {
            _body.CopyTo(memory);
            return _body.Length;
        }
    }

    public class MethodOutboundFrame : OutboundFrame
    {
        private readonly MethodBase _method;

        public MethodOutboundFrame(int channel, MethodBase method) : base(FrameType.FrameMethod, channel)
        {
            _method = method;
        }

        public override int GetMinimumPayloadBufferSize()
        {
            // class id (2 bytes) + method id (2 bytes) + arguments (X bytes)
            return 4 + _method.GetRequiredBufferSize();
        }

        public override int WritePayload(Memory<byte> memory)
        {
            NetworkOrderSerializer.WriteUInt16(memory, _method.ProtocolClassId);
            NetworkOrderSerializer.WriteUInt16(memory.Slice(2), _method.ProtocolMethodId);
            var argWriter = new MethodArgumentWriter(memory.Slice(4));
            _method.WriteArgumentsTo(ref argWriter);
            argWriter.Flush();
            return 4 + argWriter.Offset;
        }
    }

    public class EmptyOutboundFrame : OutboundFrame
    {
        public EmptyOutboundFrame() : base(FrameType.FrameHeartbeat, 0)
        {
        }

        public override int GetMinimumPayloadBufferSize()
        {
            return 0;
        }

        public override int WritePayload(Memory<byte> memory)
        {
            return 0;
        }
    }

    public abstract class OutboundFrame : Frame
    {
        public int ByteCount { get; private set; } = 0;
        protected OutboundFrame(FrameType type, int channel) : base(type, channel)
        {
        }

        public void WriteTo(Memory<byte> memory)
        {
            memory.Span[0] = (byte)Type;
            NetworkOrderSerializer.WriteUInt16(memory.Slice(1), (ushort)Channel);
            int bytesWritten = WritePayload(memory.Slice(7));
            NetworkOrderSerializer.WriteUInt32(memory.Slice(3), (uint)bytesWritten);
            memory.Span[bytesWritten + 7] = Constants.FrameEnd;
            ByteCount = bytesWritten + 8;
        }

        public abstract int WritePayload(Memory<byte> memory);
        public abstract int GetMinimumPayloadBufferSize();
        public int GetMinimumBufferSize()
        {
            return 8 + GetMinimumPayloadBufferSize();
        }
    }

    public sealed class InboundFrame : Frame, IDisposable
    {
        private readonly IMemoryOwner<byte> _payload;
        private InboundFrame(FrameType type, int channel, IMemoryOwner<byte> payload, int payloadSize) : base(type, channel, payload.Memory.Slice(0, payloadSize))
        {
            _payload = payload;
        }

        private static void ProcessProtocolHeader(Stream reader)
        {
            try
            {
                byte b1 = (byte)reader.ReadByte();
                byte b2 = (byte)reader.ReadByte();
                byte b3 = (byte)reader.ReadByte();
                if (b1 != 'M' || b2 != 'Q' || b3 != 'P')
                {
                    throw new MalformedFrameException("Invalid AMQP protocol header from server");
                }

                int transportHigh = reader.ReadByte();
                int transportLow = reader.ReadByte();
                int serverMajor = reader.ReadByte();
                int serverMinor = reader.ReadByte();
                throw new PacketNotRecognizedException(transportHigh, transportLow, serverMajor, serverMinor);
            }
            catch (EndOfStreamException)
            {
                // Ideally we'd wrap the EndOfStreamException in the
                // MalformedFrameException, but unfortunately the
                // design of MalformedFrameException's superclass,
                // ProtocolViolationException, doesn't permit
                // this. Fortunately, the call stack in the
                // EndOfStreamException is largely irrelevant at this
                // point, so can safely be ignored.
                throw new MalformedFrameException("Invalid AMQP protocol header from server");
            }
        }

        public static InboundFrame ReadFrom(Stream reader)
        {
            int type;

            try
            {
                type = reader.ReadByte();
                if (type == -1)
                {
                    throw new EndOfStreamException("Reached the end of the stream. Possible authentication failure.");
                }
            }
            catch (IOException ioe) when
                (ioe.InnerException != null
                && (ioe.InnerException is SocketException)
                && ((SocketException)ioe.InnerException).SocketErrorCode == SocketError.TimedOut)
            {
                throw ioe.InnerException;
            }

            if (type == 'A')
            {
                // Probably an AMQP protocol header, otherwise meaningless
                ProcessProtocolHeader(reader);
            }

            using IMemoryOwner<byte> headerMemory = MemoryPool<byte>.Shared.Rent(6);
            Memory<byte> headerSlice = headerMemory.Memory.Slice(0, 6);
            reader.Read(headerSlice);
            int channel = NetworkOrderDeserializer.ReadUInt16(headerSlice);
            int payloadSize = NetworkOrderDeserializer.ReadInt32(headerSlice.Slice(2)); // FIXME - throw exn on unreasonable value
            IMemoryOwner<byte> payload = MemoryPool<byte>.Shared.Rent(payloadSize);
            int bytesRead = 0;
            try
            {
                while (bytesRead < payloadSize)
                {
                    bytesRead += reader.Read(payload.Memory[bytesRead..payloadSize]);
                }
            }
            catch (Exception)
            {
                // Early EOF.
                throw new MalformedFrameException($"Short frame - expected to read {payloadSize} bytes, only got {bytesRead} bytes");
            }

            int frameEndMarker = reader.ReadByte();
            if (frameEndMarker != Constants.FrameEnd)
            {
                throw new MalformedFrameException("Bad frame end marker: " + frameEndMarker);
            }

            return new InboundFrame((FrameType)type, channel, payload, payloadSize);
        }

        public void Dispose()
        {
            if (_payload is IMemoryOwner<byte>)
            {
                _payload.Dispose();
            }
        }
    }

    public class Frame
    {
        public Frame(FrameType type, int channel)
        {
            Type = type;
            Channel = channel;
            Payload = null;
        }

        public Frame(FrameType type, int channel, ReadOnlyMemory<byte> payload)
        {
            Type = type;
            Channel = channel;
            Payload = payload;
        }

        public int Channel { get; }

        public ReadOnlyMemory<byte> Payload { get; }

        public FrameType Type { get; }

        public override string ToString()
        {
            return string.Format("(type={0}, channel={1}, {2} bytes of payload)",
                Type,
                Channel,
                Payload.Length.ToString());
        }

        public bool IsMethod()
        {
            return Type == FrameType.FrameMethod;
        }
        public bool IsHeader()
        {
            return Type == FrameType.FrameHeader;
        }
        public bool IsBody()
        {
            return Type == FrameType.FrameBody;
        }
        public bool IsHeartbeat()
        {
            return Type == FrameType.FrameHeartbeat;
        }
    }

    public enum FrameType : int
    {
        FrameMethod = 1,
        FrameHeader = 2,
        FrameBody = 3,
        FrameHeartbeat = 8,
        FrameEnd = 206,
        FrameMinSize = 4096
    }
}
