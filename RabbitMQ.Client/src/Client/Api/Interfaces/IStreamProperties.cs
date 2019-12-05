using System.Collections.Generic;

namespace RabbitMQ.Client
{
    /// <summary>
    /// Common AMQP Stream content-class headers interface,
    ///spanning the union of the functionality offered by versions 0-8, 0-8qpid, 0-9 and 0-9-1 of AMQP.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The specification code generator provides
    /// protocol-version-specific implementations of this interface. To
    /// obtain an implementation of this interface in a
    /// protocol-version-neutral way, use IModel.CreateStreamProperties().
    /// </para>
    /// <para>
    /// Each property is readable, writable and clearable: a cleared
    /// property will not be transmitted over the wire. Properties on a fresh instance are clear by default.
    /// </para>
    /// </remarks>
    public interface IStreamProperties : IContentHeader
    {
        /// <summary>
        /// MIME content encoding.
        /// </summary>
        string ContentEncoding { get; set; }

        /// <summary>
        /// MIME content type.
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Message header field table.
        /// </summary>
        IDictionary<string, object> Headers { get; set; }

        /// <summary>
        /// Message priority, 0 to 9.
        /// </summary>
        byte Priority { get; set; }

        /// <summary>
        /// Message timestamp.
        /// </summary>
        AmqpTimestamp Timestamp { get; set; }

        /// <summary>
        /// Clear the <see cref="ContentEncoding"/> property.
        /// </summary>
        void ClearContentEncoding();

        /// <summary>
        /// Clear the <see cref="ContentType"/> property.
        /// </summary>
        void ClearContentType();

        /// <summary>
        /// Clear the <see cref="Headers"/> property.
        /// </summary>
        void ClearHeaders();

        /// <summary>
        /// Clear the <see cref="Priority"/> property.
        /// </summary>
        void ClearPriority();

        /// <summary>
        /// Clear the <see cref="Timestamp"/> property.
        /// </summary>
        void ClearTimestamp();

        /// <summary>
        /// Returns true if the <see cref="ContentEncoding"/> property is present.
        /// </summary>
        bool IsContentEncodingPresent();

        /// <summary>
        /// Returns true if the <see cref="ContentType"/> property is present.
        /// </summary>
        bool IsContentTypePresent();

        /// <summary>
        /// Returns true if the <see cref="Headers"/> property is present.
        /// </summary>
        bool IsHeadersPresent();

        /// <summary>
        /// Returns true if the <see cref="Priority"/> property is present.
        /// </summary>
        bool IsPriorityPresent();

        /// <summary>
        /// Returns true if the <see cref="Timestamp"/> property is present.
        /// </summary>
        bool IsTimestampPresent();
    }
}
