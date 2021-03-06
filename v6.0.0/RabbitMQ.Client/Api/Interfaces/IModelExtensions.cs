using System;
using System.Collections.Generic;

namespace RabbitMQ.Client
{
    public static class IModelExensions
    {
        /// <summary>Start a Basic content-class consumer.</summary>
        public static string BasicConsume(
            this IModel model,
            IBasicConsumer consumer,
            string queue,
            bool autoAck = false,
            string consumerTag = "",
            bool noLocal = false,
            bool exclusive = false,
            IDictionary<string, object> arguments = null)
        {
            return model.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);
        }

        /// <summary>Start a Basic content-class consumer.</summary>
        public static string BasicConsume(this IModel model, string queue, bool autoAck, IBasicConsumer consumer)
        {
            return model.BasicConsume(queue, autoAck, "", false, false, null, consumer);
        }

        /// <summary>Start a Basic content-class consumer.</summary>
        public static string BasicConsume(this IModel model, string queue,
            bool autoAck,
            string consumerTag,
            IBasicConsumer consumer)
        {
            return model.BasicConsume(queue, autoAck, consumerTag, false, false, null, consumer);
        }

        /// <summary>Start a Basic content-class consumer.</summary>
        public static string BasicConsume(this IModel model, string queue,
            bool autoAck,
            string consumerTag,
            IDictionary<string, object> arguments,
            IBasicConsumer consumer)
        {
            return model.BasicConsume(queue, autoAck, consumerTag, false, false, arguments, consumer);
        }

        /// <summary>
        /// (Extension method) Convenience overload of BasicPublish.
        /// </summary>
        /// <remarks>
        /// The publication occurs with mandatory=false and immediate=false.
        /// </remarks>
        public static void BasicPublish(this IModel model, PublicationAddress addr, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            model.BasicPublish(addr.ExchangeName, addr.RoutingKey, basicProperties: basicProperties, body: body);
        }

        /// <summary>
        /// (Extension method) Convenience overload of BasicPublish.
        /// </summary>
        /// <remarks>
        /// The publication occurs with mandatory=false
        /// </remarks>
        public static void BasicPublish(this IModel model, string exchange, string routingKey, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            model.BasicPublish(exchange, routingKey, false, basicProperties, body);
        }

        /// <summary>
        /// (Spec method) Convenience overload of BasicPublish.
        /// </summary>
        public static void BasicPublish(this IModel model, string exchange, string routingKey, bool mandatory = false, IBasicProperties basicProperties = null, ReadOnlyMemory<byte> body = default)
        {
            model.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);
        }

        /// <summary>
        /// (Spec method) Declare a queue.
        /// </summary>
        public static QueueDeclareOk QueueDeclare(this IModel model, string queue = "", bool durable = false, bool exclusive = true,
            bool autoDelete = true, IDictionary<string, object> arguments = null)
        {
            return model.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        /// <summary>
        /// (Extension method) Bind an exchange to an exchange.
        /// </summary>
        public static void ExchangeBind(this IModel model, string destination, string source, string routingKey, IDictionary<string, object> arguments = null)
        {
            model.ExchangeBind(destination, source, routingKey, arguments);
        }

        /// <summary>
        /// (Extension method) Like exchange bind but sets nowait to true. 
        /// </summary>
        public static void ExchangeBindNoWait(this IModel model, string destination, string source, string routingKey, IDictionary<string, object> arguments = null)
        {
            model.ExchangeBindNoWait(destination, source, routingKey, arguments);
        }

        /// <summary>
        /// (Spec method) Declare an exchange.
        /// </summary>
        public static void ExchangeDeclare(this IModel model, string exchange, string type, bool durable = false, bool autoDelete = false,
            IDictionary<string, object> arguments = null)
        {
            model.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
        }

        /// <summary>
        /// (Extension method) Like ExchangeDeclare but sets nowait to true. 
        /// </summary>
        public static void ExchangeDeclareNoWait(this IModel model, string exchange, string type, bool durable = false, bool autoDelete = false,
            IDictionary<string, object> arguments = null)
        {
            model.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);
        }

        /// <summary>
        /// (Spec method) Unbinds an exchange.
        /// </summary>
        public static void ExchangeUnbind(this IModel model, string destination,
            string source,
            string routingKey,
            IDictionary<string, object> arguments = null)
        {
            model.ExchangeUnbind(destination, source, routingKey, arguments);
        }

        /// <summary>
        /// (Spec method) Deletes an exchange.
        /// </summary>
        public static void ExchangeDelete(this IModel model, string exchange, bool ifUnused = false)
        {
            model.ExchangeDelete(exchange, ifUnused);
        }

        /// <summary>
        /// (Extension method) Like ExchangeDelete but sets nowait to true.
        /// </summary>
        public static void ExchangeDeleteNoWait(this IModel model, string exchange, bool ifUnused = false)
        {
            model.ExchangeDeleteNoWait(exchange, ifUnused);
        }

        /// <summary>
        /// (Spec method) Binds a queue.
        /// </summary>
        public static void QueueBind(this IModel model, string queue, string exchange, string routingKey, IDictionary<string, object> arguments = null)
        {
            model.QueueBind(queue, exchange, routingKey, arguments);
        }

        /// <summary>
        /// (Spec method) Deletes a queue.
        /// </summary>
        public static uint QueueDelete(this IModel model, string queue, bool ifUnused = false, bool ifEmpty = false)
        {
            return model.QueueDelete(queue, ifUnused, ifEmpty);
        }

        /// <summary>
        /// (Extension method) Like QueueDelete but sets nowait to true.
        /// </summary>
        public static void QueueDeleteNoWait(this IModel model, string queue, bool ifUnused = false, bool ifEmpty = false)
        {
            model.QueueDeleteNoWait(queue, ifUnused, ifEmpty);
        }

        /// <summary>
        /// (Spec method) Unbinds a queue.
        /// </summary>
        public static void QueueUnbind(this IModel model, string queue, string exchange, string routingKey, IDictionary<string, object> arguments = null)
        {
            model.QueueUnbind(queue, exchange, routingKey, arguments);
        }
    }
}
