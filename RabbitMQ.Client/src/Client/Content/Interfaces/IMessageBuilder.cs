using System.Collections.Generic;
using System.IO;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Interface for constructing application messages.
    /// </summary>
    /// <remarks>
    /// Subinterfaces provide specialized data-writing methods. This
    /// base interface deals with the lowest common denominator:
    /// bytes, with no special encodings for higher-level objects.
    /// </remarks>
    public interface IMessageBuilder
    {
        /// <summary>
        /// Retrieve the <see cref="Stream"/> being used to construct the message body.
        /// </summary>
        Stream BodyStream { get; }

        /// <summary>
        /// Retrieves the dictionary that will be used to construct the message header table.
        /// It is of type <see cref="IDictionary{TKey,TValue}" />.
        /// </summary>
        IDictionary<string, object> Headers { get; }

        /// <summary>
        /// Finish and retrieve the content body for transmission.
        /// </summary>
        byte[] GetContentBody();

        /// <summary>
        /// Finish and retrieve the content header for transmission.
        /// </summary>
        IContentHeader GetContentHeader();

        /// <summary>
        /// Returns the default MIME content type for messages this instance constructs,
        /// or null if none is available or relevant.
        /// </summary>
        string GetDefaultContentType();

        /// <summary>
        /// Write a single byte into the message body, without encoding or interpretation.
        /// </summary>
        IMessageBuilder RawWrite(byte value);

        /// <summary>
        /// Write a byte array into the message body, without encoding or interpretation.
        /// </summary>
        IMessageBuilder RawWrite(byte[] bytes);

        /// <summary>
        /// Write a section of a byte array into the message body, without encoding or interpretation.
        /// </summary>
        IMessageBuilder RawWrite(byte[] bytes, int offset, int length);
    }
}
