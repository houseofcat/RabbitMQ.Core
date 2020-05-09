using System.Text;

namespace RabbitMQ.Client.Impl
{
    abstract class MethodBase : IMethod
    {
        public abstract bool HasContent { get; }

        /// <summary>
        /// Retrieves the class ID number of this method, as defined in the AMQP specification XML.
        /// </summary>
        public abstract ushort ProtocolClassId { get; }

        /// <summary>
        /// Retrieves the method ID number of this method, as defined in the AMQP specification XML.
        /// </summary>
        public abstract ushort ProtocolMethodId { get; }

        /// <summary>
        /// Retrieves the name of this method - for debugging use.
        /// </summary>
        public abstract string ProtocolMethodName { get; }

        public abstract void AppendArgumentDebugStringTo(StringBuilder stringBuilder);
        public abstract void ReadArgumentsFrom(ref MethodArgumentReader reader);
        public abstract void WriteArgumentsTo(ref MethodArgumentWriter writer);
        public abstract int GetRequiredBufferSize();
    }
}
