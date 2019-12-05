namespace RabbitMQ.Client
{
    public interface IBasicPublishBatch
    {
        void Add(string exchange, string routingKey, bool mandatory, IBasicProperties properties, byte[] body);
        void Publish();
    }
}
