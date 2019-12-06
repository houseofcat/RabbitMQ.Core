using System.Collections.Generic;

namespace RabbitMQ.Util
{
    public class SetQueue<T>
    {
        private readonly HashSet<T> members = new HashSet<T>();
        private readonly LinkedList<T> queue = new LinkedList<T>();

        public bool Enqueue(T item)
        {
            if (members.Contains(item))
            {
                return false;
            }
            members.Add(item);
            queue.AddLast(item);
            return true;
        }

        public T Dequeue()
        {
            if (queue.Count == 0)
            {
                return default;
            }
            T item = queue.First.Value;
            queue.RemoveFirst();
            members.Remove(item);
            return item;
        }

        public bool Contains(T item)
        {
            return members.Contains(item);
        }

        public bool IsEmpty()
        {
            return members.Count == 0;
        }

        public bool Remove(T item)
        {
            queue.Remove(item);
            return members.Remove(item);
        }

        public void Clear()
        {
            queue.Clear();
            members.Clear();
        }
    }
}
