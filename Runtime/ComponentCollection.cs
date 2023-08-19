using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Abg.Entities
{
    internal interface IComponentCollection
    {
        void Add();
        void RemoveAt(int index);
        void CopyTo(int sourceIndex, IComponentCollection destCollection, int destIndex);
    }

    public struct ComponentAccessor<T> : IDisposable
    {
        private readonly ComponentCollection<T> collection;

        internal ComponentAccessor(ComponentCollection<T> collection)
        {
            this.collection = collection;
            collection.Lock();
        }
        
        public ref T this[int indexInCollection]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref collection.components.Buffer[indexInCollection];
        }

        public void Dispose()
        {
            collection.Unlock();
        }
    }
    
    internal class ComponentCollection<T> : IComponentCollection
    {
        private int lockCounter;
        internal ArrayList<T> components = new ArrayList<T>(128);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent(int indexInCollection)
        {
            return ref components.Buffer[indexInCollection];
        }

        public void Add()
        {
            components.Add(default);
        }

        public void RemoveAt(int index)
        {
            components.UnorderedRemoveAt(index);
        }

        public void CopyTo(int sourceIndex, IComponentCollection destCollection, int destIndex)
        {
            ((ComponentCollection<T>)destCollection).components[destIndex] = components[sourceIndex];
        }

        public void Set(int index, T component)
        {
            components[index] = component;
        }

        public void Lock()
        {
            Interlocked.Increment(ref lockCounter);
        }

        public void Unlock()
        {
            Interlocked.Decrement(ref lockCounter);
        }
    }
}