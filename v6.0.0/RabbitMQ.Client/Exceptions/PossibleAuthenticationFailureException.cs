using System;

namespace RabbitMQ.Client.Exceptions
{
    /// <summary> Thrown when the likely cause is  an
    /// authentication failure. </summary>
#if !NETSTANDARD1_5
    [Serializable]
#endif
    public class PossibleAuthenticationFailureException : RabbitMQClientException
    {
        public PossibleAuthenticationFailureException(string msg, Exception inner) : base(msg, inner)
        {
        }
        public PossibleAuthenticationFailureException(string msg) : base(msg)
        {
        }

        protected PossibleAuthenticationFailureException() : base()
        {
        }
    }
}
