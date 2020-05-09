using RabbitMQ.Client.Framing.Impl;

namespace RabbitMQ.Client.Impl
{
    ///<summary>Normal ISession implementation used during normal channel operation.</summary>
    internal class Session : SessionBase
    {
        private readonly CommandAssembler _assembler;

        public Session(Connection connection, int channelNumber)
            : base(connection, channelNumber)
        {
            _assembler = new CommandAssembler(connection.Protocol);
        }

        public override void HandleFrame(InboundFrame frame)
        {
            using (Command cmd = _assembler.HandleFrame(frame))
            {
                if (cmd != null)
                {
                    OnCommandReceived(cmd);
                }
            }
        }
    }
}
