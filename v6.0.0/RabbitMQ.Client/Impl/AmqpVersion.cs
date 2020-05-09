namespace RabbitMQ.Client.Framing.Impl
{
    /// <summary>Represents a version of the AMQP specification.</summary>
    /// <remarks>
    /// <para>
    /// Vendor-specific variants of particular official specification
    /// versions exist: this class simply represents the AMQP
    /// specification version, and does not try to represent
    /// information about any custom variations involved.
    /// </para>
    /// <para>
    /// AMQP version 0-8 peers sometimes advertise themselves as
    /// version 8-0: for this reason, this class's constructor
    /// special-cases 8-0, rewriting it at construction time to be 0-8 instead.
    /// </para>
    /// </remarks>
    internal class AmqpVersion
    {
        /// <summary>
        /// Construct an <see cref="AmqpVersion"/> from major and minor version numbers.
        /// </summary>
        /// <remarks>
        /// Converts major=8 and minor=0 into major=0 and minor=8. Please see the class comment.
        /// </remarks>
        public AmqpVersion(int major, int minor)
        {
            if (major == 8 && minor == 0)
            {
                // The AMQP 0-8 spec confusingly defines the version
                // as 8-0. This maps the latter to the former, for
                // cases where our peer might be confused.
                major = 0;
                minor = 8;
            }
            Major = major;
            Minor = minor;
        }

        /// <summary>
        /// The AMQP specification major version number.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// The AMQP specification minor version number.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Implement value-equality comparison.
        /// </summary>
        public override bool Equals(object other)
        {
            return (other is AmqpVersion version) && (version.Major == Major) && (version.Minor == Minor);
        }

        /// <summary>
        /// Implement hashing as for value-equality.
        /// </summary>
        public override int GetHashCode()
        {
            return 31 * Major.GetHashCode() + Minor.GetHashCode();
        }

        /// <summary>
        /// Format appropriately for display.
        /// </summary>
        /// <remarks>
        /// The specification currently uses "MAJOR-MINOR" as a display format.
        /// </remarks>
        public override string ToString()
        {
            return Major + "-" + Minor;
        }
    }
}
