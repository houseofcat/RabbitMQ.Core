using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace RabbitMQ.Client.Logging
{
    public sealed class RabbitMqConsoleEventListener : EventListener, IDisposable
    {
        public RabbitMqConsoleEventListener()
        {
            EnableEvents(RabbitMqClientEventSource.Log, EventLevel.Informational, RabbitMqClientEventSource.Keywords.Log);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            foreach (object pl in eventData.Payload)
            {
                if (pl is IDictionary<string, object> dict)
                {
                    var rex = new RabbitMqExceptionDetail(dict);
                    Console.WriteLine("{0}: {1}", eventData.Level, rex.ToString());
                }
                else
                {
                    Console.WriteLine("{0}: {1}", eventData.Level, pl.ToString());
                }
            }
        }

        public override void Dispose()
        {
            DisableEvents(RabbitMqClientEventSource.Log);
        }
    }
}
