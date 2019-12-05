using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace RabbitMQ.Client.Impl
{
    public class Command
    {
        // EmptyFrameSize, 8 = 1 + 2 + 4 + 1
        // - 1 byte of frame type
        // - 2 bytes of channel number
        // - 4 bytes of frame payload length
        // - 1 byte of payload trailer FrameEnd byte
        private const int EmptyFrameSize = 8;
        private readonly MemoryStream m_body;
        private static readonly byte[] m_emptyByteArray = new byte[0];

        static Command()
        {
            CheckEmptyFrameSize();
        }

        public Command() : this(null, null, null)
        {
        }

        public Command(MethodBase method) : this(method, null, null)
        {
        }

        public Command(MethodBase method, ContentHeaderBase header, byte[] body)
        {
            Method = method;
            Header = header;
            if (body != null)
            {
                m_body = new MemoryStream(body);
            }
            else
            {
                m_body = new MemoryStream();
            }
        }

        public byte[] Body
        {
            get { return ConsolidateBody(); }
        }

        public ContentHeaderBase Header { get; set; }

        public MethodBase Method { get; set; }

        public static void CheckEmptyFrameSize()
        {
            var f = new EmptyOutboundFrame();
            var stream = new MemoryStream();
            var writer = new NetworkBinaryWriter(stream);
            f.WriteTo(writer);
            long actualLength = stream.Length;

            if (EmptyFrameSize != actualLength)
            {
                string message =
                    string.Format("EmptyFrameSize is incorrect - defined as {0} where the computed value is in fact {1}.",
                        EmptyFrameSize,
                        actualLength);
                throw new ProtocolViolationException(message);
            }
        }

        public void AppendBodyFragment(byte[] fragment)
        {
            if (fragment != null)
            {
                m_body.Write(fragment, 0, fragment.Length);
            }
        }

        public byte[] ConsolidateBody()
        {
            return m_body.Length == 0 ? m_emptyByteArray : m_body.ToArray();
        }

        public void Transmit(int channelNumber, Connection connection)
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

        public void TransmitAsSingleFrame(int channelNumber, Connection connection)
        {
            connection.WriteFrame(new MethodOutboundFrame(channelNumber, Method));
        }

        public void TransmitAsFrameSet(int channelNumber, Connection connection)
        {
            var frames = new List<OutboundFrame>
            {
                new MethodOutboundFrame(channelNumber, Method)
            };
            if (Method.HasContent)
            {
                var body = ConsolidateBody(); // Cache, since the property is compiled.

                frames.Add(new HeaderOutboundFrame(channelNumber, Header, body.Length));
                var frameMax = (int)Math.Min(int.MaxValue, connection.FrameMax);
                var bodyPayloadMax = (frameMax == 0) ? body.Length : frameMax - EmptyFrameSize;
                for (int offset = 0; offset < body.Length; offset += bodyPayloadMax)
                {
                    var remaining = body.Length - offset;
                    var count = (remaining < bodyPayloadMax) ? remaining : bodyPayloadMax;
                    frames.Add(new BodySegmentOutboundFrame(channelNumber, body, offset, count));
                }
            }

            connection.WriteFrameSet(frames);
        }

        public static List<OutboundFrame> CalculateFrames(int channelNumber, Connection connection, IList<Command> commands)
        {
            var frames = new List<OutboundFrame>();

            foreach (var cmd in commands)
            {
                frames.Add(new MethodOutboundFrame(channelNumber, cmd.Method));
                if (cmd.Method.HasContent)
                {
                    var body = cmd.Body;// var body = ConsolidateBody(); // Cache, since the property is compiled.

                    frames.Add(new HeaderOutboundFrame(channelNumber, cmd.Header, body.Length));
                    var frameMax = (int)Math.Min(int.MaxValue, connection.FrameMax);
                    var bodyPayloadMax = (frameMax == 0) ? body.Length : frameMax - EmptyFrameSize;
                    for (int offset = 0; offset < body.Length; offset += bodyPayloadMax)
                    {
                        var remaining = body.Length - offset;
                        var count = (remaining < bodyPayloadMax) ? remaining : bodyPayloadMax;
                        frames.Add(new BodySegmentOutboundFrame(channelNumber, body, offset, count));
                    }
                }
            }

            return frames;
        }
    }
}
