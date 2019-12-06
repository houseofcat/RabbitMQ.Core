using System.Collections;
using System.Collections.Generic;

namespace RabbitMQ.Util
{
    internal class SynchronizedList<T> : IList<T>
    {
        private readonly IList<T> list;

        internal SynchronizedList()
            : this(new List<T>())
        {
        }

        internal SynchronizedList(IList<T> list)
        {
            this.list = list;
            SyncRoot = new object();
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return list.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return list.IsReadOnly; }
        }

        public T this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return list[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    list[index] = value;
                }
            }
        }

        public object SyncRoot { get; }

        public void Add(T item)
        {
            lock (SyncRoot)
            {
                list.Add(item);
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                list.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                list.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            lock (SyncRoot)
            {
                return list.Remove(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return list.GetEnumerator();
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return list.GetEnumerator();
            }
        }

        public int IndexOf(T item)
        {
            lock (SyncRoot)
            {
                return list.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (SyncRoot)
            {
                list.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                list.RemoveAt(index);
            }
        }
    }
}
