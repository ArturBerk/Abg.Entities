using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Abg.Entities
{
   public struct ArrayList<T> : IList<T>, IList, IReadOnlyCollection<T>
    {
        private const int MinSize = 4;

        public T[] Buffer;
        public int Count;

        public ArrayList(int initialSize)
        {
            Count = 0;
            Buffer = new T[initialSize];
        }

        public ArrayList(ICollection<T> collection)
        {
            Buffer = new T[collection.Count];

            collection.CopyTo(Buffer, 0);

            Count = Buffer.Length;
        }
        
        public ArrayList(ArrayList<T> collection)
        {
            Buffer = new T[collection.Count];
            Count = collection.Count;

            Array.Copy(collection.Buffer, 0, Buffer, 0, Count);
            //System.Buffer.BlockCopy(collection.Buffer, 0, Buffer, 0, Count);
        }

        public ArrayList(IList<T> listCopy)
        {
            Buffer = new T[listCopy.Count];

            listCopy.CopyTo(Buffer, 0);

            Count = listCopy.Count;
        }

        public ref T this[int i] => ref Buffer[i];

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SetAt(int index, T value)
        {
            if (Buffer.Length <= index) AllocateMore(index + 1);
            if (Count <= index) Count = index + 1;
            Buffer[index] = value;
        }

        public void Add(T item)
        {
            if (Count == Buffer.Length)
                AllocateMore();

            Buffer[Count++] = item;
        }

        public void Add(ref T item)
        {
            if (Count == Buffer.Length)
                AllocateMore();

            Buffer[Count++] = item;
        }

        public int Add(object value)
        {
            Add((T) value);
            return Buffer.Length - 1;
        }

        public void Clear()
        {
            Array.Clear(Buffer, 0, Buffer.Length);

            Count = 0;
        }

        public bool Contains(object value)
        {
            return Contains((T) value);
        }

        public int IndexOf(object value)
        {
            return IndexOf((T) value);
        }

        public void Insert(int index, object value)
        {
            Insert(index, (T) value);
        }

        public void Remove(object value)
        {
            Remove((T) value);
        }

        public bool Contains(T item)
        {
            var index = IndexOf(item);

            return index != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(Buffer, 0, array, arrayIndex, Count);
        }

        public int IndexOf(T item)
        {
            if (Count == 0) return -1;
            var comp = EqualityComparer<T>.Default;

            for (var index = Count - 1; index >= 0; --index)
                if (comp.Equals(Buffer[index], item))
                    return index;

            return -1;
        }

        public void Insert(int index, T item)
        {
            if (Count == Buffer.Length) AllocateMore();

            Array.Copy(Buffer, index, Buffer, index + 1, Count - index);

            Buffer[index] = item;
            ++Count;
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);

            if (index == -1)
                return false;

            RemoveAt(index);

            return true;
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        int ICollection.Count => Count;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        int ICollection<T>.Count => Count;

        public bool IsReadOnly => false;

        object IList.this[int index] { get =>Buffer[index];
            set => Buffer[index] = (T) value;
        }

        public void RemoveAt(int index)
        {
            if (index == --Count)
                return;

            Array.Copy(Buffer, index + 1, Buffer, index, Count - index);

            Buffer[Count] = default(T);
        }

        public bool IsFixedSize => false;

        T IList<T>.this[int index]
        {
            get
            {
                return Buffer[index];
            }
            set
            {
                Buffer[index] = value;
            }
        }

        public void AddRange(IEnumerable<T> items, int count)
        {
            AddRange(items.GetEnumerator(), count);
        }

        public void AddRange(IEnumerator<T> items, int count)
        {
            if (Count + count >= Buffer.Length)
                AllocateMore(Count + count);

            while (items.MoveNext())
                Buffer[Count++] = items.Current;
        }

        public void AddRange(ArrayList<T> items)
        {
            if (items.Count == 0) return;

            if (Count + items.Count >= Buffer.Length)
                AllocateMore(Count + items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                Add(ref items[i]);
            }
        }

        public void AddRange(ICollection<T> items)
        {
            AddRange(items.GetEnumerator(), items.Count);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void AddRange(ref ArrayList<T> items)
        {
            AddRange(items.Buffer, items.Count);
        }

        public void AddRange(T[] items, int count)
        {
            if (count == 0) return;

            if (Count + count >= Buffer.Length)
                AllocateMore(Count + count);

            Array.Copy(items, 0, Buffer, Count, count);
            Count += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(T[] items)
        {
            AddRange(items, items.Length);
        }

        public void AddMany(ref T value, int count)
        {
            if (count == 0) return;
            var newSize = Count + count;
            if (newSize >= Buffer.Length)
                AllocateMore(newSize);
            for (int i = Count; i < newSize; i++)
            {
                Buffer[i] = value;
            }
            Count += count;
        }

        /// <summary>
        ///     Careful, you could keep on holding references you don't want to hold to anymore
        ///     Use DeepClear in case.
        /// </summary>
        public void FastClear()
        {
            Count = 0;
        }

        public ArrayEnumerator<T> GetEnumerator()
        {
            return new ArrayEnumerator<T>(Buffer, Count);
        }

        public void Release()
        {
            Count = 0;
            Buffer = null;
        }

        public void Resize(int newSize)
        {
            if (newSize > Buffer.Length)
            {
                AllocateMore(newSize);
            }
            Count = newSize;
        }
        
        public void EnsureCapacity(int newSize)
        {
            if (newSize <= Buffer.Length)
                return;
            AllocateMore(newSize);
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(Buffer, 0, Count, comparer);
        }

        public bool UnorderedRemove(T item)
        {
            var index = IndexOf(item);

            if (index == -1)
                return false;

            UnorderedRemoveAt(index);

            return true;
        }

        public bool UnorderedRemoveAt(int index)
        {
            if (index == --Count)
            {
                Buffer[Count] = default(T);
                return false;
            }

            Buffer[index] = Buffer[Count];
            Buffer[Count] = default(T);

            return true;
        }

        private void AllocateMore()
        {
            Array.Resize(ref Buffer, Buffer.Length == 0 ? 4 : (int)Math.Ceiling(Buffer.Length * 1.4));
        }

        private void AllocateMore(int newSize)
        {
            var oldLength = Buffer.Length;

            while (oldLength < newSize)
                oldLength = (int)Math.Ceiling(oldLength * 1.4);

            Array.Resize(ref Buffer, oldLength);
        }

        public void Trim()
        {
            if (Count < Buffer.Length)
                Resize(Count);
        }

        public T[] ToArray()
        {
            var result = new T[Count];
            Array.Copy(Buffer, result, Count);
            return result;
        }

        int IReadOnlyCollection<T>.Count => Count;
    }

    public struct ArrayEnumerator<T> : IEnumerator<T>
    {

        public ArrayEnumerator(T[] buffer, int size)
        {
            this.size = size;
            counter = -1;
            this.buffer = buffer;
        }

        object IEnumerator.Current => buffer[counter];

        T IEnumerator<T>.Current => buffer[counter];

        public void Dispose()
        {
            buffer = null;
        }

        public bool MoveNext()
        {
            if (counter < size - 1)
            {
                ++counter;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            counter = -1;
        }

        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        void IEnumerator.Reset()
        {
            Reset();
        }

        private T[] buffer;
        private int counter;
        private readonly int size;
    }

    public struct ArraySlice<T>
    {
        private T[] array;
        private int offset;
        private int count;

        public int Length => count;

        public ArraySlice(T[] array, int offset, int count)
        {
            this.array = array;
            this.offset = offset;
            this.count = count;
        }

        public ref T this[int index] => ref array[index + offset];
    }
}