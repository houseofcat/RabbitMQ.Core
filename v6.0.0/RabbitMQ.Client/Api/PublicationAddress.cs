using System.Text.RegularExpressions;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Container for an exchange name, exchange type and
    /// routing key, usable as the target address of a message to be published.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The syntax used for the external representation of instances
    /// of this class is compatible with QPid's "Reply-To" field
    /// pseudo-URI format. The pseudo-URI format is
    /// (exchange-type)://(exchange-name)/(routing-key), where
    /// exchange-type is one of the permitted exchange type names (see
    /// class ExchangeType), exchange-name must be present but may be
    /// empty, and routing-key must be present but may be empty.
    /// </para>
    /// <para>
    /// The syntax is as it is solely for compatibility with QPid's
    /// existing usage of the ReplyTo field; the AMQP specifications
    /// 0-8 and 0-9 do not define the format of the field, and do not
    /// define any format for the triple (exchange name, exchange
    /// type, routing key) that could be used instead.
    /// </para>
    /// </remarks>
    public class PublicationAddress
    {
        /// <summary>
        /// Regular expression used to extract the exchange-type,
        /// exchange-name and routing-key from a string.
        /// </summary>
        public static readonly Regex PSEUDO_URI_PARSER = new Regex("^([^:]+)://([^/]*)/(.*)$");

        /// <summary>
        ///  Creates a new instance of the <see cref="PublicationAddress"/>.
        /// </summary>
        /// <param name="exchangeType">Exchange type.</param>
        /// <param name="exchangeName">Exchange name.</param>
        /// <param name="routingKey">Routing key.</param>
        public PublicationAddress(string exchangeType, string exchangeName, string routingKey)
        {
            ExchangeType = exchangeType;
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
        }

        /// <summary>
        /// Retrieve the exchange name.
        /// </summary>
        public string ExchangeName { get; }

        /// <summary>
        /// Retrieve the exchange type string.
        /// </summary>
        public string ExchangeType { get; }

        /// <summary>
        ///Retrieve the routing key.
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        /// Parse a <see cref="PublicationAddress"/> out of the given string,
        ///  using the <see cref="PSEUDO_URI_PARSER"/> regex.
        /// </summary>
        public static PublicationAddress Parse(string uriLikeString)
        {
            Match match = PSEUDO_URI_PARSER.Match(uriLikeString);
            if (match.Success)
            {
                return new PublicationAddress(match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value);
            }
            return null;
        }

        /// <summary>
        /// Reconstruct the "uri" from its constituents.
        /// </summary>
        public override string ToString()
        {
            return ExchangeType + "://" + ExchangeName + "/" + RoutingKey;
        }
    }
}
