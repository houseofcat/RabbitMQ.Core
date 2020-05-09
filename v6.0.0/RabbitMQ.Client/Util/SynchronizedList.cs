using System.Collections;
using System.Collections.Generic;

namespace RabbitMQ.Util
{
    internal class SynchronizedList<T> : IList<T>
    {
        private readonly IList<T> _list;

        internal SynchronizedList()
            : this(new List<T>())
        {
        }

        internal SynchronizedList(IList<T> list)
        {
            _list = list;
            SyncRoot = new object();
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return _list.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return _list.IsReadOnly; }
        }

        public T this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return _list[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    _list[index] = value;
                }
            }
        }

        public object SyncRoot { get; }

        public void Add(T item)
        {
            lock (SyncRoot)
            {
                _list.Add(item);
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                _list.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return _list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                _list.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            lock (SyncRoot)
            {
                return _list.Remove(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return _list.GetEnumerator();
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return _list.GetEnumerator();
            }
        }

        public int IndexOf(T item)
        {
            lock (SyncRoot)
            {
                return _list.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (SyncRoot)
            {
                _list.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                _list.RemoveAt(index);
            }
        }
    }
}
