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

        protected UnknownClassOrMethodException(string message) : base(message)
        {
        }

        protected UnknownClassOrMethodException() : base()
        {
        }

        protected UnknownClassOrMethodException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        ///<summary>The AMQP content-class ID.</summary>
        public ushort ClassId { get; }

        ///<summary>The AMQP method ID within the content-class, or 0 if none.</summary>
        public ushort MethodId { get; }

        public override ushort ReplyCode
        {
            get { return Constants.NotImplemented; }
        }

#pragma warning disable IDE0071 // Simplify interpolation
        public override string ToString()
        {
            return MethodId == 0
                ? $"{base.ToString()}<{ClassId}>"

                : $"{base.ToString()}<{ClassId}.{MethodId}>";
        }
#pragma warning restore IDE0071 // Simplify interpolation
    }
}
