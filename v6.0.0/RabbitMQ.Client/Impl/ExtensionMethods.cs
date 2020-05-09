using System;
using System.Collections.Generic;
using System.IO;
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
        public static T RandomItem<T>(this IList<T> list)
        {
            int n = list.Count;
            if (n == 0)
            {
                return default;
            }

            int hashCode = Math.Abs(Guid.NewGuid().GetHashCode());
            return list[hashCode % n];
        }

        public static ArraySegment<byte> GetBufferSegment(this MemoryStream ms)
        {
            byte[] buffer = ms.GetBuffer();
            return new ArraySegment<byte>(buffer, 0, (int)ms.Position);
        }
    }
}
