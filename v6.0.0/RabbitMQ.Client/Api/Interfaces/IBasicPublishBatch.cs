using System;

namespace RabbitMQ.Client
{
    public interface IBasicPublishBatch
    {
        void Add(string exchange, string routingKey, bool mandatory, IBasicProperties properties, ReadOnlyMemory<byte> body);
        void Publish();
    }
}
