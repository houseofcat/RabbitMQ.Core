using System.Text;

namespace RabbitMQ.Client.Impl
{
    public abstract class MethodBase : IMethod
    {
        public abstract bool HasContent { get; }

        /// <summary>
        /// Retrieves the class ID number of this method, as defined in the AMQP specification XML.
        /// </summary>
        public abstract int ProtocolClassId { get; }

        /// <summary>
        /// Retrieves the method ID number of this method, as defined in the AMQP specification XML.
        /// </summary>
        public abstract int ProtocolMethodId { get; }

        /// <summary>
        /// Retrieves the name of this method - for debugging use.
        /// </summary>
        public abstract string ProtocolMethodName { get; }

        public abstract void AppendArgumentDebugStringTo(StringBuilder stringBuilder);
        public abstract void ReadArgumentsFrom(MethodArgumentReader reader);
        public abstract void WriteArgumentsTo(MethodArgumentWriter writer);
    }
}
