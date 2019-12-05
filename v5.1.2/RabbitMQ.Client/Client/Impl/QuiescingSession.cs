using RabbitMQ.Client.Framing.Impl;

namespace RabbitMQ.Client.Impl
{
    ///<summary>Small ISession implementation used during channel quiescing.</summary>
    public class QuiescingSession : SessionBase
    {
        public ShutdownEventArgs m_reason;

        public QuiescingSession(Connection connection,
            int channelNumber,
            ShutdownEventArgs reason)
            : base(connection, channelNumber)
        {
            m_reason = reason;
        }

        public override void HandleFrame(InboundFrame frame)
        {
            if (frame.IsMethod())
            {
                MethodBase method = Connection.Protocol.DecodeMethodFrom(frame.GetReader());
                if ((method.ProtocolClassId == ChannelCloseOk.ClassId)
                    && (method.ProtocolMethodId == ChannelCloseOk.MethodId))
                {
                    // This is the reply we were looking for. Release
                    // the channel with the reason we were passed in
                    // our constructor.
                    Close(m_reason);
                }
                else if ((method.ProtocolClassId == ChannelClose.ClassId)
                         && (method.ProtocolMethodId == ChannelClose.MethodId))
                {
                    // We're already shutting down the channel, so
                    // just send back an ok.
                    Transmit(CreateChannelCloseOk());
                }
            }

            // Either a non-method frame, or not what we were looking
            // for. Ignore it - we're quiescing.
        }

        protected Command CreateChannelCloseOk()
        {
            return new Command(new ConnectionCloseOk());
        }
    }
}
