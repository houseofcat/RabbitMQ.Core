using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace RabbitMQ.Client.Logging
{
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
            if (ex.TryGetValue("InnerException", out object inner))
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
            return string.Format("Exception: {0}\r\n{1}\r\n\r\n{2}\r\nInnerException:\r\n{3}", Type, Message, StackTrace, InnerException);
        }
    }
}
