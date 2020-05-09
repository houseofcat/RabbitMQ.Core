using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RabbitMQ.Client.Impl
{
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Returns a random item from the list.
        /// </summary>
        /// <typeparam name="T">Element item type</typeparam>
        /// <param name="list">Input list</param>
        /// <returns></returns>
        public static T RandomItem<T>(this IList<T> list)
        {
            int n = list.Count;
            if (n == 0)
            {
                return default;
            }

            int hashCode = Math.Abs(Guid.NewGuid().GetHashCode());
            return list.ElementAt<T>(hashCode % n);
        }

        internal static ArraySegment<byte> GetBufferSegment(this MemoryStream ms)
        {
            byte[] buffer = ms.GetBuffer();
            return new ArraySegment<byte>(buffer, 0, (int)ms.Position);
        }
    }
}
