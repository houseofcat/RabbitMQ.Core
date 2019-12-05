using System.Collections.Generic;
using System.IO;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    /// Interface for analyzing application messages.
    /// </summary>
    /// <remarks>
    /// Subinterfaces provide specialized data-reading methods. This
    /// base interface deals with the lowest common denominator:
    /// bytes, with no special encodings for higher-level objects.
    /// </remarks>
    public interface IMessageReader
    {
        /// <summary>
        /// Retrieve the message body, as a byte array.
        /// </summary>
        byte[] BodyBytes { get; }

        /// <summary>
        /// Retrieve the <see cref="Stream"/> being used to read from the message body.
        /// </summary>
        Stream BodyStream { get; }

        /// <summary>
        /// Retrieves the content header properties of the message being read. Is of type <seealso cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        IDictionary<string, object> Headers { get; }

        /// <summary>
        /// Read a single byte from the body stream, without encoding or interpretation.
        /// Returns -1 for end-of-stream.
        /// </summary>
        int RawRead();

        /// <summary>
        /// Read bytes from the body stream into a section of
        /// an existing byte array, without encoding or
        /// interpretation. Returns the number of bytes read from the
        /// body and written into the target array, which may be less
        /// than the number requested if the end-of-stream is reached.
        /// </summary>
        int RawRead(byte[] target, int offset, int length);
    }
}
