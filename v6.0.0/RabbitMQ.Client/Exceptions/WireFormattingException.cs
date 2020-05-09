using System;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary> Thrown when the wire-formatting code cannot encode a
    /// particular .NET value to AMQP protocol format.  </summary>
#if !NETSTANDARD1_5
    [Serializable]
#endif
    public class WireFormattingException : ProtocolViolationException
    {
        ///<summary>Construct a WireFormattingException with no
        ///particular offender (i.e. null)</summary>
        public WireFormattingException(string message) : this(message, null)
        {
        }

        ///<summary>Construct a WireFormattingException with the given
        ///offender</summary>
        public WireFormattingException(string message, object offender)
            : base(message)
        {
            Offender = offender;
        }

        ///<summary>Object which this exception is complaining about;
        ///may be null if no particular offender exists</summary>
        public object Offender { get; }
    }
}
