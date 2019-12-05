using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public class RecordedExchange : RecordedNamedEntity
    {
        private string type;

        public RecordedExchange(AutorecoveringModel model, string name) : base(model, name)
        {
        }

        public IDictionary<string, object> Arguments { get; private set; }
        public bool Durable { get; private set; }
        public bool IsAutoDelete { get; private set; }

        public string Type
        {
            get { return type; }
        }

        public void Recover()
        {
            ModelDelegate.ExchangeDeclare(Name, type, Durable, IsAutoDelete, Arguments);
        }

        public override string ToString()
        {
            return String.Format("{0}: name = '{1}', type = '{2}', durable = {3}, autoDelete = {4}, arguments = '{5}'",
                GetType().Name, Name, type, Durable, IsAutoDelete, Arguments);
        }

        public RecordedExchange WithArguments(IDictionary<string, object> value)
        {
            Arguments = value;
            return this;
        }

        public RecordedExchange WithAutoDelete(bool value)
        {
            IsAutoDelete = value;
            return this;
        }

        public RecordedExchange WithDurable(bool value)
        {
            Durable = value;
            return this;
        }

        public RecordedExchange WithType(string value)
        {
            type = value;
            return this;
        }
    }
}
