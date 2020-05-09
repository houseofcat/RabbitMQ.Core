using System;
using System.Threading.Tasks;

namespace RabbitMQ.Client.Impl
{
    interface IConsumerDispatcher
    {
        bool IsShutdown { get; }

        void HandleBasicConsumeOk(IBasicConsumer consumer,
                             string consumerTag);

        void HandleBasicDeliver(IBasicConsumer consumer,
                            string consumerTag,
                            ulong deliveryTag,
                            bool redelivered,
                            string exchange,
                            string routingKey,
                            IBasicProperties basicProperties,
                            ReadOnlyMemory<byte> body);

        void HandleBasicCancelOk(IBasicConsumer consumer,
                            string consumerTag);

        void HandleBasicCancel(IBasicConsumer consumer,
                          string consumerTag);

        void HandleModelShutdown(IBasicConsumer consumer,
            ShutdownEventArgs reason);

        void Quiesce();

        Task Shutdown(IModel model);
    }
}
