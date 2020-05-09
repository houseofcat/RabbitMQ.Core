using RabbitMQ.Client.Framing.Impl;
using System;

namespace RabbitMQ.Client.Impl
{
    public class RecoveryAwareModel : Model, IFullModel, IRecoverable
    {
        public RecoveryAwareModel(ISession session) : base(session)
        {
            ActiveDeliveryTagOffset = 0;
            MaxSeenDeliveryTag = 0;
        }

        public ulong ActiveDeliveryTagOffset { get; private set; }
        public ulong MaxSeenDeliveryTag { get; private set; }

        public void InheritOffsetFrom(RecoveryAwareModel other)
        {
            ActiveDeliveryTagOffset = other.ActiveDeliveryTagOffset + other.MaxSeenDeliveryTag;
            MaxSeenDeliveryTag = 0;
        }

        public override void HandleBasicGetOk(ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            uint messageCount,
            IBasicProperties basicProperties,
            ReadOnlyMemory<byte> body)
        {
            if (deliveryTag > MaxSeenDeliveryTag)
            {
                MaxSeenDeliveryTag = deliveryTag;
            }

            base.HandleBasicGetOk(
                OffsetDeliveryTag(deliveryTag),
                redelivered,
                exchange,
                routingKey,
                messageCount,
                basicProperties,
                body);
        }

        public override void HandleBasicDeliver(string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties basicProperties,
            ReadOnlyMemory<byte> body)
        {
            if (deliveryTag > MaxSeenDeliveryTag)
            {
                MaxSeenDeliveryTag = deliveryTag;
            }

            base.HandleBasicDeliver(consumerTag,
                OffsetDeliveryTag(deliveryTag),
                redelivered,
                exchange,
                routingKey,
                basicProperties,
                body);
        }

        public override void BasicAck(ulong deliveryTag,
            bool multiple)
        {
            ulong realTag = deliveryTag - ActiveDeliveryTagOffset;
            if (realTag > 0 && realTag <= deliveryTag)
            {
                base.BasicAck(realTag, multiple);
            }
        }

        public override void BasicNack(ulong deliveryTag,
            bool multiple,
            bool requeue)
        {
            ulong realTag = deliveryTag - ActiveDeliveryTagOffset;
            if (realTag > 0 && realTag <= deliveryTag)
            {
                base.BasicNack(realTag, multiple, requeue);
            }
        }

        public override void BasicReject(ulong deliveryTag,
            bool requeue)
        {
            ulong realTag = deliveryTag - ActiveDeliveryTagOffset;
            if (realTag > 0 && realTag <= deliveryTag)
            {
                base.BasicReject(realTag, requeue);
            }
        }

        protected ulong OffsetDeliveryTag(ulong deliveryTag)
        {
            return deliveryTag + ActiveDeliveryTagOffset;
        }
    }
}
