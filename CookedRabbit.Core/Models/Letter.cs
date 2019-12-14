namespace CookedRabbit.Core
{
    public class Letter
    {
        public Envelope Envelope { get; set; }
        public ulong LetterId { get; set; }

        public LetterMetadata LetterMetadata { get; set; }
        public byte[] Body { get; set; }

        public Letter(string exchange, string routingKey, byte[] data, LetterMetadata metadata = null, RoutingOptions routingOptions = null)
        {
            Envelope = new Envelope
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                RoutingOptions = routingOptions ?? DefaultRoutingOptions()
            };
            Body = data;
            LetterMetadata = metadata;
        }

        public Letter(string exchange, string routingKey, byte[] data, string id, RoutingOptions routingOptions = null)
        {
            Envelope = new Envelope
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                RoutingOptions = routingOptions ?? DefaultRoutingOptions()
            };
            Body = data;
            if (string.IsNullOrWhiteSpace(id))
            {
                LetterMetadata = new LetterMetadata { Id = id };
            }
        }

        public Letter(string exchange, string routingKey, byte[] data, string id, byte priority)
        {
            Envelope = new Envelope
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                RoutingOptions = DefaultRoutingOptions(priority)
            };
            Body = data;
            if (string.IsNullOrWhiteSpace(id))
            {
                LetterMetadata = new LetterMetadata { Id = id };
            }
        }

        public RoutingOptions DefaultRoutingOptions(byte priority = 0)
        {
            return new RoutingOptions
            {
                DeliveryMode = 2,
                Mandatory = false,
                PriorityLevel = priority
            };
        }
    }
}
