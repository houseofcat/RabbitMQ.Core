using RabbitMQ.Client.Framing;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary> Thrown when the server sends a frame along a channel
    /// that we do not currently have a Session entry in our
    /// SessionManager for. </summary>
    public class ChannelErrorException : HardProtocolException
    {
        public ChannelErrorException(int channel)
            : base($"Frame received for invalid channel {channel}")
        {
            Channel = channel;
        }

        ///<summary>The channel number concerned.</summary>
        public int Channel { get; private set; }

        public override ushort ReplyCode
        {
            get { return Constants.ChannelError; }
        }
    }
}
