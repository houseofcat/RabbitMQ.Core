using System;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary> Thrown when a SessionManager cannot allocate a new
    /// channel number, or the requested channel number is already in
    /// use. </summary>
#if !NETSTANDARD1_5
    [Serializable]
#endif
    public class ChannelAllocationException : ProtocolViolationException
    {
        /// <summary>
        /// Indicates that there are no more free channels.
        /// </summary>
        public ChannelAllocationException()
            : base("The connection cannot support any more channels. Consider creating a new connection")
        {
            Channel = -1;
        }

        /// <summary>
        /// Indicates that the specified channel is in use
        /// </summary>
        /// <param name="channel">The requested channel number</param>
        public ChannelAllocationException(int channel)
            : base($"The Requested Channel ({channel}) is already in use.")
        {
            Channel = channel;
        }

        public ChannelAllocationException(string message) : base(message)
        {
        }

        public ChannelAllocationException(string message, Exception inner) : base(message, inner)
        {
        }

        ///<summary>Retrieves the channel number concerned; will
        ///return -1 in the case where "no more free channels" is
        ///being signaled, or a non-negative integer when "channel is
        ///in use" is being signaled.</summary>
        public int Channel { get; }
    }
}
