using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Impl
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns a random item from the list.
        /// </summary>
        /// <typeparam name="T">Element item type</typeparam>
        /// <param name="list">Input list</param>
        /// <returns></returns>
        public static T RandomItem<T>(this IList<T> list)
        {
            if (list.Count == 0)
            {
                return default;
            }

            var hashCode = Math.Abs(Guid.NewGuid().GetHashCode());
            return list[hashCode % list.Count];
        }
    }
}
