using System;
using System.IO;

namespace RabbitMQ.Client.Exceptions
{
    ///<summary>Thrown when no connection could be opened during a
    ///ConnectionFactory.CreateConnection attempt.</summary>
#if !NETSTANDARD1_5
    [Serializable]
#endif
    public class BrokerUnreachableException : IOException
    {
        ///<summary>Construct a BrokerUnreachableException. The inner exception is
        ///an AggregateException holding the errors from multiple connection attempts.</summary>
        public BrokerUnreachableException(Exception Inner)
            : base("None of the specified endpoints were reachable", Inner)
        {
        }

        public BrokerUnreachableException() : base()
        {
        }

        public BrokerUnreachableException(string message) : base(message)
        {
        }

        public BrokerUnreachableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BrokerUnreachableException(string message, int hresult) : base(message, hresult)
        {
        }
    }
}
