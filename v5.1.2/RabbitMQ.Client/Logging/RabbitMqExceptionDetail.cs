namespace RabbitMQ.Client.Logging
{
    using System;
    using System.Collections.Generic;
#if NET451
    using Microsoft.Diagnostics.Tracing;
#else
    using System.Diagnostics.Tracing;
#endif

    [EventData]
    public class RabbitMqExceptionDetail
    {
        public RabbitMqExceptionDetail(Exception ex)
        {
            Type = ex.GetType().FullName;
            Message = ex.Message;
            StackTrace = ex.StackTrace;
            if (ex.InnerException != null)
            {
                InnerException = ex.InnerException.ToString();
            }
        }

        public RabbitMqExceptionDetail(IDictionary<string, object> ex)
        {
            Type = ex["Type"].ToString();
            Message = ex["Message"].ToString();
            StackTrace = ex["StackTrace"].ToString();
            object inner;
            if (ex.TryGetValue("InnerException", out inner))
            {
                InnerException = inner.ToString();
            }
        }

        public string Type { get; }
        public string Message { get; }
        public string StackTrace { get; }
        public string InnerException { get; }

        public override string ToString()
        {
            return String.Format("Exception: {0}\r\n{1}\r\n\r\n{2}\r\nInnerException:\r\n{3}", Type, Message, StackTrace, InnerException);
        }
    }
}
