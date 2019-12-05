using RabbitMQ.Client.Framing.Impl;

namespace RabbitMQ.Client.Impl
{
    ///<summary>Normal ISession implementation used during normal channel operation.</summary>
    public class Session : SessionBase
    {
        public CommandAssembler m_assembler;

        public Session(Connection connection, int channelNumber)
            : base(connection, channelNumber)
        {
            m_assembler = new CommandAssembler(connection.Protocol);
        }

        public override void HandleFrame(InboundFrame frame)
        {
            Command cmd = m_assembler.HandleFrame(frame);
            if (cmd != null)
            {
                OnCommandReceived(cmd);
            }
        }
    }
}
