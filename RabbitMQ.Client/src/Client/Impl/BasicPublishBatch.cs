namespace RabbitMQ.Client.Impl
{
    using RabbitMQ.Client;
    using RabbitMQ.Client.Framing.Impl;
    using System.Collections.Generic;

    public class BasicPublishBatch : IBasicPublishBatch
    {
        private readonly List<Command> commands = new List<Command>();
        private readonly ModelBase model;

        internal BasicPublishBatch(ModelBase model)
        {
            this.model = model;
        }

        public void Add(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, byte[] body)
        {
            var bp = basicProperties ?? model.CreateBasicProperties();
            var method = new BasicPublish
            {
                m_exchange = exchange,
                m_routingKey = routingKey,
                m_mandatory = mandatory
            };

            commands.Add(new Command(method, (ContentHeaderBase)bp, body));
        }

        public void Publish()
        {
            model.SendCommands(commands);
        }
    }
}