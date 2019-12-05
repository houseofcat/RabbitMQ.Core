using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Events
{
    public abstract class BaseExceptionEventArgs : EventArgs
    {
        ///<summary>Wrap an exception thrown by a callback.</summary>
        protected BaseExceptionEventArgs(Exception exception)
        {
            Detail = new Dictionary<string, object>();
            Exception = exception;
        }

        ///<summary>Access helpful information about the context in
        ///which the wrapped exception was thrown.</summary>
        public IDictionary<string, object> Detail { get; }

        ///<summary>Access the wrapped exception.</summary>
        public Exception Exception { get; }

        public IDictionary<string, object> UpdateDetails(IDictionary<string, object> other)
        {
            foreach (var pair in other)
            {
                Detail[pair.Key] = pair.Value;
            }
            return Detail;
        }
    }

    ///<summary>Describes an exception that was thrown during the
    ///library's invocation of an application-supplied callback
    ///handler.</summary>
    ///<remarks>
    ///<para>
    /// When an exception is thrown from a callback registered with
    /// part of the RabbitMQ .NET client library, it is caught,
    /// packaged into a CallbackExceptionEventArgs, and passed through
    /// the appropriate IModel's or IConnection's CallbackException
    /// event handlers. If an exception is thrown in a
    /// CallbackException handler, it is silently swallowed, as
    /// CallbackException is the last chance to handle these kinds of
    /// exception.
    ///</para>
    ///<para>
    /// Code constructing CallbackExceptionEventArgs instances will
    /// usually place helpful information about the context of the
    /// call in the IDictionary available through the Detail property.
    ///</para>
    ///</remarks>
    public class CallbackExceptionEventArgs : BaseExceptionEventArgs
    {
        public CallbackExceptionEventArgs(Exception e) : base(e)
        {
        }

        public static CallbackExceptionEventArgs Build(Exception e,
                                                       string context)
        {
            var details = new Dictionary<string, object>
            {
                {"context", context}
            };
            return Build(e, details);
        }

        public static CallbackExceptionEventArgs Build(Exception e,
                                                       IDictionary<string, object> details)
        {
            var exnArgs = new CallbackExceptionEventArgs(e);
            exnArgs.UpdateDetails(details);
            return exnArgs;
        }
    }
}
