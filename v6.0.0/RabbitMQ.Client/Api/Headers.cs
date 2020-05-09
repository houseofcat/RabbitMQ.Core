namespace RabbitMQ.Client
{
    /// <summary>
    /// Convenience class providing compile-time names for standard headers.
    /// </summary>
    /// <remarks>
    /// Use the static members of this class as headers for the
    /// arguments for Queue and Exchange declaration or Consumer creation. 
    /// The broker may be extended with additional
    /// headers that do not appear in this class.
    /// </remarks>
    public static class Headers
    {
        /// <summary>
        /// x-max-priority header
        /// </summary>
        public const string XMaxPriority = "x-max-priority";

        /// <summary>
        /// x-max-length header
        /// </summary>
        public const string XMaxLength = "x-max-length";

        /// <summary>
        /// x-max-length-bytes header
        /// </summary>
        public const string XMaxLengthInBytes = "x-max-length-bytes";

        /// <summary>
        /// x-dead-letter-exchange header
        /// </summary>
        public const string XDeadLetterExchange = "x-dead-letter-exchange";

        /// <summary>
        /// x-dead-letter-routing-key header
        /// </summary>
        public const string XDeadLetterRoutingKey = "x-dead-letter-routing-key";

        /// <summary>
        /// x-message-ttl header
        /// </summary>
        public const string XMessageTTL = "x-message-ttl";

        /// <summary>
        /// x-expires header
        /// </summary>
        public const string XExpires = "x-expires";

        /// <summary>
        /// alternate-exchange header
        /// </summary>
        public const string AlternateExchange = "alternate-exchange";

        /// <summary>
        /// x-priority header
        /// </summary>
        public const string XPriority = "x-priority";

        /// <summary>
        /// x-queue-mode header.
        /// Available modes: "default" and "lazy"
        /// </summary>
        public const string XQueueMode = "x-queue-mode";

        // quorum
        /// <summary>
        /// x-queue-type header.
        /// Available types: "quorum" and "classic"(default)
        /// </summary>
        public const string XQueueType = "x-queue-type";

        /// <summary>
        /// x-quorum-initial-group-size header.
        /// Use to control the number of quorum queue members
        /// </summary>
        public const string XQuorumInitialGroupSize = "x-quorum-initial-group-size";

        // true/false
        /// <summary>
        /// x-single-active-consumer header.
        /// Available modes: true and false(default).
        /// Allows to have only one consumer at a time consuming from a queue
        /// and to fail over to another registered consumer in case the active one is cancelled or dies
        ///  </summary>
        public const string XSingleActiveConsumer = "x-single-active-consumer";

        /// <summary>
        /// x-overflow header.
        /// Available strategies: "reject-publish" and "drop-head"(default).
        /// Allows to configure strategy when <see cref="XMaxLength"/> or <see cref="XMaxLengthInBytes"/> hits limits
        /// </summary>
        public const string XOverflow = "x-overflow";
    }
}
