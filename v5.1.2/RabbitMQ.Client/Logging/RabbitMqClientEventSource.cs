namespace RabbitMQ.Client.Logging
{
    using System;
    using System.Diagnostics.Tracing;

    [EventSource(Name = "rabbitmq-dotnet-client")]
    public sealed class RabbitMqClientEventSource : EventSource
    {
        public static class Keywords
        {
            public const EventKeywords Log = (EventKeywords)1;
        }

        public RabbitMqClientEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        public static RabbitMqClientEventSource Log = new RabbitMqClientEventSource();

        [Event(1, Message = "INFO", Keywords = Keywords.Log, Level = EventLevel.Informational)]
        public void Info(string message)
        {
            if (IsEnabled())
            {
                WriteEvent(1, message);
            }
        }

        [Event(2, Message = "WARN", Keywords = Keywords.Log, Level = EventLevel.Warning)]
        public void Warn(string message)
        {
            if (IsEnabled())
            {
                WriteEvent(2, message);
            }
        }

        [Event(3, Message = "ERROR", Keywords = Keywords.Log, Level = EventLevel.Error)]
        public void Error(string message, RabbitMqExceptionDetail ex)
        {
            if (IsEnabled())
            {
                WriteEvent(3, message, ex);
            }
        }

        [NonEvent]
        public void Error(string message, Exception ex)
        {
            Error(message, new RabbitMqExceptionDetail(ex));
        }
    }
}