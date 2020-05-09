using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public class RecordedConsumer : RecordedEntity
    {
        public RecordedConsumer(AutorecoveringModel model, string queue) : base(model)
        {
            Queue = queue;
        }

        public IDictionary<string, object> Arguments { get; set; }
        public bool AutoAck { get; set; }
        public IBasicConsumer Consumer { get; set; }
        public string ConsumerTag { get; set; }
        public bool Exclusive { get; set; }
        public string Queue { get; set; }

        public string Recover()
        {
            ConsumerTag = ModelDelegate.BasicConsume(Queue, AutoAck,
                ConsumerTag, false, Exclusive,
                Arguments, Consumer);

            return ConsumerTag;
        }

        public RecordedConsumer WithArguments(IDictionary<string, object> value)
        {
            Arguments = value;
            return this;
        }

        public RecordedConsumer WithAutoAck(bool value)
        {
            AutoAck = value;
            return this;
        }

        public RecordedConsumer WithConsumer(IBasicConsumer value)
        {
            Consumer = value;
            return this;
        }

        public RecordedConsumer WithConsumerTag(string value)
        {
            ConsumerTag = value;
            return this;
        }

        public RecordedConsumer WithExclusive(bool value)
        {
            Exclusive = value;
            return this;
        }

        public RecordedConsumer WithQueue(string value)
        {
            Queue = value;
            return this;
        }
    }
}
