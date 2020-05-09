using RabbitMQ.Client.Framing;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary>
    /// Thrown when the protocol handlers detect an unknown class
    /// number or method number.
    /// </summary>
    public class UnknownClassOrMethodException : HardProtocolException
    {
        public UnknownClassOrMethodException(ushort classId, ushort methodId)
            : base($"The Class or Method <{classId}.{methodId}> is unknown")
        {
            ClassId = classId;
            MethodId = methodId;
        }

        ///<summary>The AMQP content-class ID.</summary>
        public ushort ClassId { get; private set; }

        ///<summary>The AMQP method ID within the content-class, or 0 if none.</summary>
        public ushort MethodId { get; private set; }

        public override ushort ReplyCode
        {
            get { return Constants.NotImplemented; }
        }

        public override string ToString()
        {
            return MethodId == 0
                ? $"{base.ToString()}<{ClassId}>"
                : $"{base.ToString()}<{ClassId}.{MethodId}>";
        }
    }
}
