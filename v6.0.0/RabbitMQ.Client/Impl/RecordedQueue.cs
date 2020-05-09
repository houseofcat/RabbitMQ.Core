using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public class RecordedQueue : RecordedNamedEntity
    {
        private IDictionary<string, object> _arguments;
        private bool _durable;
        private bool _exclusive;

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
            _arguments = value;
            return this;
        }

        public RecordedQueue AutoDelete(bool value)
        {
            IsAutoDelete = value;
            return this;
        }

        public RecordedQueue Durable(bool value)
        {
            _durable = value;
            return this;
        }

        public RecordedQueue Exclusive(bool value)
        {
            _exclusive = value;
            return this;
        }

        public void Recover()
        {
            QueueDeclareOk ok = ModelDelegate.QueueDeclare(NameToUseForRecovery, _durable,
                _exclusive, IsAutoDelete,
                _arguments);
            Name = ok.QueueName;
        }

        public RecordedQueue ServerNamed(bool value)
        {
            IsServerNamed = value;
            return this;
        }

        public override string ToString()
        {
            return string.Format("{0}: name = '{1}', durable = {2}, exlusive = {3}, autoDelete = {4}, arguments = '{5}'",
                GetType().Name, Name, _durable, _exclusive, IsAutoDelete, _arguments);
        }
    }
}
