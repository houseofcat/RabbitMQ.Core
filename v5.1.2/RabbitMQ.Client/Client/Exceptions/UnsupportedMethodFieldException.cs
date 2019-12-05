using System;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary>Thrown when the model cannot transmit a method field
    /// because the version of the protocol the model is implementing
    /// does not contain a definition for the field in
    /// question.</summary>
    public class UnsupportedMethodFieldException : NotSupportedException
    {
        public UnsupportedMethodFieldException(string methodName, string fieldName)
        {
            MethodName = methodName;
            FieldName = fieldName;
        }

        public UnsupportedMethodFieldException()
        {
        }

        public UnsupportedMethodFieldException(string message) : base(message)
        {
        }

        public UnsupportedMethodFieldException(string message, Exception innerException) : base(message, innerException)
        {
        }

        ///<summary>The name of the unsupported field.</summary>
        public string FieldName { get; }

        ///<summary>The name of the method involved.</summary>
        public string MethodName { get; }
    }
}
