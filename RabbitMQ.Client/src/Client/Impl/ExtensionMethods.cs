using System;
using System.Collections.Generic;
using System.Linq;

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
            var n = list.Count;
            if (n == 0)
            {
                return default(T);
            }

            var hashCode = Math.Abs(Guid.NewGuid().GetHashCode());
            return list.ElementAt<T>(hashCode % n);
        }
    }
}
