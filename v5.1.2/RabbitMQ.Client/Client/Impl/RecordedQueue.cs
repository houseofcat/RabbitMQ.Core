using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public class RecordedQueue : RecordedNamedEntity
    {
        private IDictionary<string, object> arguments;
        private bool durable;
        private bool exclusive;

        public RecordedQueue(AutorecoveringModel model, string name) : base(model, name)
        {
        }

        public bool IsAutoDelete { get; private set; }
        public bool IsServerNamed { get; private set; }

        protected string NameToUseForRecovery
        {
            get
            {
                if (IsServerNamed)
                {
                    return string.Empty;
                }
                else
                {
                    return Name;
                }
            }
        }

        public RecordedQueue Arguments(IDictionary<string, object> value)
        {
            arguments = value;
            return this;
        }

        public RecordedQueue AutoDelete(bool value)
        {
            IsAutoDelete = value;
            return this;
        }

        public RecordedQueue Durable(bool value)
        {
            durable = value;
            return this;
        }

        public RecordedQueue Exclusive(bool value)
        {
            exclusive = value;
            return this;
        }

        public void Recover()
        {
            QueueDeclareOk ok = ModelDelegate.QueueDeclare(NameToUseForRecovery, durable,
                exclusive, IsAutoDelete,
                arguments);
            Name = ok.QueueName;
        }

        public RecordedQueue ServerNamed(bool value)
        {
            IsServerNamed = value;
            return this;
        }

        public override string ToString()
        {
            return String.Format("{0}: name = '{1}', durable = {2}, exlusive = {3}, autoDelete = {4}, arguments = '{5}'",
                GetType().Name, Name, durable, exclusive, IsAutoDelete, arguments);
        }
    }
}
