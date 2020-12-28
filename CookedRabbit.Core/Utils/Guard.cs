using System;
using System.Collections.Generic;
using System.Linq;

namespace CookedRabbit.Core.Utils
{
    /// <summary>
    /// A class to store methods that aid in validation.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Throws exception if object is null.
        /// </summary>
        /// <param name="argumentValue"></param>
        /// <param name="argumentName"></param>
        public static void AgainstNull(object argumentValue, string argumentName)
        {
            if (argumentValue is null) throw new ArgumentNullException(argumentName);
        }

        /// <summary>
        /// Throws exception if string is null or empty.
        /// </summary>
        /// <param name="argumentValue"></param>
        /// <param name="argumentName"></param>
        public static void AgainstNullOrEmpty(string argumentValue, string argumentName)
        {
            if (string.IsNullOrEmpty(argumentValue)) throw new ArgumentException($"{argumentName} can't be null or empty.");
        }

        /// <summary>
        /// Throws an exception if both strings are null or empty.
        /// </summary>
        /// <param name="argumentValue"></param>
        /// <param name="argumentName"></param>
        /// <param name="secondArgumentValue"></param>
        /// <param name="secondArgumentName"></param>
        public static void AgainstBothNullOrEmpty(string argumentValue, string argumentName, string secondArgumentValue, string secondArgumentName)
        {
            if (string.IsNullOrEmpty(argumentValue) && string.IsNullOrEmpty(secondArgumentValue)) throw new ArgumentException($"{argumentName} and {secondArgumentName} can't both be null or empty.");
        }

        /// <summary>
        /// Throws an exception if IEnumerable is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="argumentValue"></param>
        /// <param name="argumentName"></param>
        public static void AgainstNullOrEmpty<T>(IEnumerable<T> argumentValue, string argumentName)
        {
            if (argumentValue?.Any() != true) throw new ArgumentException($"{argumentName} can't be null or empty.");
        }

        /// <summary>
        /// Throws an exception if IEnumerable is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="argumentValue"></param>
        /// <param name="argumentName"></param>
        public static void AgainstNullOrEmpty<T>(ICollection<T> argumentValue, string argumentName)
        {
            if (argumentValue?.Any() != true) throw new ArgumentException($"{argumentName} can't be null or empty.");
        }

        /// <summary>
        /// Throws an exception if bool is true.
        /// </summary>
        /// <param name="argumentValue"></param>
        /// <param name="argumentName"></param>
        public static void AgainstTrue(bool argumentValue, string argumentName)
        {
            if (argumentValue) throw new ArgumentException($"{argumentName} can't be true for this method.");
        }

        /// <summary>
        /// Throws an exception if bool is false.
        /// </summary>
        /// <param name="argumentValue"></param>
        /// <param name="argumentName"></param>
        public static void AgainstFalse(bool argumentValue, string argumentName)
        {
            if (!argumentValue) throw new ArgumentException($"{argumentName} can't be false for this method.");
        }
    }
}
