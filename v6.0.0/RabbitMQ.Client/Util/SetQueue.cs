using System.Collections.Generic;

namespace RabbitMQ.Util
{
    class SetQueue<T>
    {
        private readonly HashSet<T> _members = new HashSet<T>();
        private readonly LinkedList<T> _queue = new LinkedList<T>();

        public bool Enqueue(T item)
        {
            if (_members.Contains(item))
            {
                return false;
            }
            _members.Add(item);
            _queue.AddLast(item);
            return true;
        }

        public T Dequeue()
        {
            if (_queue.Count == 0)
            {
                return default;
            }
            T item = _queue.First.Value;
            _queue.RemoveFirst();
            _members.Remove(item);
            return item;
        }

        public bool Contains(T item)
        {
            return _members.Contains(item);
        }

        public bool IsEmpty()
        {
            return _members.Count == 0;
        }

        public bool Remove(T item)
        {
            _queue.Remove(item);
            return _members.Remove(item);
        }

        public void Clear()
        {
            _queue.Clear();
            _members.Clear();
        }
    }
}
