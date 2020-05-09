using RabbitMQ.Client.Framing.Impl;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    class BasicPublishBatch : IBasicPublishBatch
    {
        private readonly List<Command> _commands = new List<Command>();
        private readonly ModelBase _model;
        internal BasicPublishBatch(ModelBase model)
        {
            _model = model;
        }

        public void Add(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, byte[] body)
        {
            IBasicProperties bp = basicProperties ?? _model.CreateBasicProperties();
            var method = new BasicPublish
            {
                _exchange = exchange,
                _routingKey = routingKey,
                _mandatory = mandatory
            };

            _commands.Add(new Command(method, (ContentHeaderBase)bp, body));
        }

        public void Publish()
        {
            _model.SendCommands(_commands);
        }
    }
}
