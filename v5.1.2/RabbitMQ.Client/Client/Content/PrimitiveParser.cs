using System;

namespace RabbitMQ.Client.Content
{
    /// <summary>
    ///     Utility class for extracting typed values from strings.
    /// </summary>
    public static class PrimitiveParser
    {
        /// <summary>
        /// Creates the protocol violation exception.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="source">The source.</param>
        /// <returns>Instance of the <see cref="ProtocolViolationException" />.</returns>
        public static Exception CreateProtocolViolationException(string targetType, object source)
        {
            string message = string.Format("Invalid conversion to {0}: {1}", targetType, source);
            return new Exception(message);
        }

        /// <summary>
        /// Attempt to parse a string representation of a <see cref="bool" />.
        /// </summary>
        /// <exception cref="ProtocolViolationException" />
        public static bool ParseBool(string value)
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            throw CreateProtocolViolationException("bool", value);
        }

        /// <summary>
        /// Attempt to parse a string representation of a <see cref="byte" />.
        /// </summary>
        /// <exception cref="ProtocolViolationException" />
        public static byte ParseByte(string value)
        {
            if (byte.TryParse(value, out byte result))
            {
                return result;
            }
            throw CreateProtocolViolationException("byte", value);
        }

        /// <summary>
        /// Attempt to parse a string representation of a <see cref="double" />.
        /// </summary>
        /// <exception cref="ProtocolViolationException" />
        public static double ParseDouble(string value)
        {
            if (double.TryParse(value, out double result))
            {
                return result;
            }
            throw CreateProtocolViolationException("double", value);
        }

        /// <summary>
        /// Attempt to parse a string representation of a <see cref="float" />.
        /// </summary>
        /// <exception cref="ProtocolViolationException" />
        public static float ParseFloat(string value)
        {
            if (float.TryParse(value, out float result))
            {
                return result;
            }
            throw CreateProtocolViolationException("float", value);
        }

        /// <summary>
        /// Attempt to parse a string representation of an <see cref="int" />.
        /// </summary>
        /// <exception cref="ProtocolViolationException" />
        public static int ParseInt(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            throw CreateProtocolViolationException("int", value);
        }

        /// <summary>
        /// Attempt to parse a string representation of a <see cref="long" />.
        /// </summary>
        /// <exception cref="ProtocolViolationException" />
        public static long ParseLong(string value)
        {
            if (long.TryParse(value, out long result))
            {
                return result;
            }
            throw CreateProtocolViolationException("long", value);
        }

        /// <summary>
        /// Attempt to parse a string representation of a <see cref="short" />.
        /// </summary>
        /// <exception cref="ProtocolViolationException" />
        public static short ParseShort(string value)
        {
            if (short.TryParse(value, out short result))
            {
                return result;
            }
            throw CreateProtocolViolationException("short", value);
        }
    }
}
