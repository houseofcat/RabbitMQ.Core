using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Impl
{
    public enum AssemblyState
    {
        ExpectingMethod,
        ExpectingContentHeader,
        ExpectingContentBody,
        Complete
    }


    public class CommandAssembler
    {
        public Command m_command;
        public ProtocolBase m_protocol;
        public ulong m_remainingBodyBytes;
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
                    {
                        if (!f.IsMethod())
                        {
                            throw new UnexpectedFrameException(f);
                        }
                        m_command.Method = m_protocol.DecodeMethodFrom(f.GetReader());
                        m_state = m_command.Method.HasContent
                            ? AssemblyState.ExpectingContentHeader
                            : AssemblyState.Complete;
                        return CompletedCommand();
                    }
                case AssemblyState.ExpectingContentHeader:
                    {
                        if (!f.IsHeader())
                        {
                            throw new UnexpectedFrameException(f);
                        }
                        NetworkBinaryReader reader = f.GetReader();
                        m_command.Header = m_protocol.DecodeContentHeaderFrom(reader);
                        m_remainingBodyBytes = m_command.Header.ReadFrom(reader);
                        UpdateContentBodyState();
                        return CompletedCommand();
                    }
                case AssemblyState.ExpectingContentBody:
                    {
                        if (!f.IsBody())
                        {
                            throw new UnexpectedFrameException(f);
                        }
                        m_command.AppendBodyFragment(f.Payload);
                        if ((ulong)f.Payload.Length > m_remainingBodyBytes)
                        {
                            throw new MalformedFrameException
                                (string.Format("Overlong content body received - {0} bytes remaining, {1} bytes received",
                                    m_remainingBodyBytes,
                                    f.Payload.Length));
                        }
                        m_remainingBodyBytes -= (ulong)f.Payload.Length;
                        UpdateContentBodyState();
                        return CompletedCommand();
                    }
                case AssemblyState.Complete:
                default:
#if NETFX_CORE
                    Debug.WriteLine("Received frame in invalid state {0}; {1}", m_state, f);
#else
                    //Trace.Fail(string.Format("Received frame in invalid state {0}; {1}", m_state, f));
#endif
                    return null;
            }
        }

        private Command CompletedCommand()
        {
            if (m_state == AssemblyState.Complete)
            {
                Command result = m_command;
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
            m_command = new Command();
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
