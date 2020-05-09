using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    internal class Command : IDisposable
    {
        // EmptyFrameSize, 8 = 1 + 2 + 4 + 1
        // - 1 byte of frame type
        // - 2 bytes of channel number
        // - 4 bytes of frame payload length
        // - 1 byte of payload trailer FrameEnd byte
        private const int EmptyFrameSize = 8;
        private static readonly byte[] s_emptyByteArray = new byte[0];
        private readonly IMemoryOwner<byte> _body;

        static Command()
        {
            CheckEmptyFrameSize();
        }

        internal Command(MethodBase method) : this(method, null, null, 0)
        {
        }

        internal Command(MethodBase method, ContentHeaderBase header, ReadOnlyMemory<byte> body)
        {
            Method = method;
            Header = header;
            Body = body;
        }

        public Command(MethodBase method, ContentHeaderBase header, IMemoryOwner<byte> body, int bodySize)
        {
            Method = method;
            Header = header;
            _body = body;
            Body = _body?.Memory.Slice(0, bodySize) ?? s_emptyByteArray;
        }

        public ReadOnlyMemory<byte> Body { get; }

        internal ContentHeaderBase Header { get; }

        internal MethodBase Method { get; }

        public static void CheckEmptyFrameSize()
        {
            var f = new EmptyOutboundFrame();
            byte[] b = new byte[f.GetMinimumBufferSize()];
            f.WriteTo(b);
            long actualLength = f.ByteCount;

            if (EmptyFrameSize != actualLength)
            {
                string message =
                    string.Format("EmptyFrameSize is incorrect - defined as {0} where the computed value is in fact {1}.",
                        EmptyFrameSize,
                        actualLength);
                throw new ProtocolViolationException(message);
            }
        }

        internal void Transmit(int channelNumber, Connection connection)
        {
            if (Method.HasContent)
            {
                TransmitAsFrameSet(channelNumber, connection);
            }
            else
            {
                TransmitAsSingleFrame(channelNumber, connection);
            }
        }

        internal void TransmitAsSingleFrame(int channelNumber, Connection connection)
        {
            connection.WriteFrame(new MethodOutboundFrame(channelNumber, Method));
        }

        internal void TransmitAsFrameSet(int channelNumber, Connection connection)
        {
            var frames = new List<OutboundFrame> { new MethodOutboundFrame(channelNumber, Method) };
            if (Method.HasContent)
            {
                frames.Add(new HeaderOutboundFrame(channelNumber, Header, Body.Length));
                int frameMax = (int)Math.Min(int.MaxValue, connection.FrameMax);
                int bodyPayloadMax = (frameMax == 0) ? Body.Length : frameMax - EmptyFrameSize;
                for (int offset = 0; offset < Body.Length; offset += bodyPayloadMax)
                {
                    int remaining = Body.Length - offset;
                    int count = (remaining < bodyPayloadMax) ? remaining : bodyPayloadMax;
                    frames.Add(new BodySegmentOutboundFrame(channelNumber, Body.Slice(offset, count)));
                }
            }

            connection.WriteFrameSet(frames);
        }


        internal static List<OutboundFrame> CalculateFrames(int channelNumber, Connection connection, IList<Command> commands)
        {
            var frames = new List<OutboundFrame>();

            foreach (Command cmd in commands)
            {
                frames.Add(new MethodOutboundFrame(channelNumber, cmd.Method));
                if (cmd.Method.HasContent)
                {
                    frames.Add(new HeaderOutboundFrame(channelNumber, cmd.Header, cmd.Body.Length));
                    int frameMax = (int)Math.Min(int.MaxValue, connection.FrameMax);
                    int bodyPayloadMax = (frameMax == 0) ? cmd.Body.Length : frameMax - EmptyFrameSize;
                    for (int offset = 0; offset < cmd.Body.Length; offset += bodyPayloadMax)
                    {
                        int remaining = cmd.Body.Length - offset;
                        int count = (remaining < bodyPayloadMax) ? remaining : bodyPayloadMax;
                        frames.Add(new BodySegmentOutboundFrame(channelNumber, cmd.Body.Slice(offset, count)));
                    }
                }
            }

            return frames;
        }

        public void Dispose()
        {
            if (_body is IMemoryOwner<byte>)
            {
                _body.Dispose();
            }
        }
    }
}
