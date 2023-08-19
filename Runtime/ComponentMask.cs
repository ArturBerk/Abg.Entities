using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Abg.Entities
{
    internal class ComponentMask : IEquatable<ComponentMask>, IDisposable
    {
        public static readonly ComponentMask Empty = new ComponentMask
        {
            data = Array.Empty<int>(),
            maxComponentIndex = -1
        };

        private const int BitsPerInt32 = 32;
        private const int MaxElementsInPool = 8;

        private int[] data;
        private int maxComponentIndex;

        private static readonly Queue<ComponentMask> pool = new Queue<ComponentMask>();

        public bool IsEmpty => maxComponentIndex < 0;

        private static ComponentMask TakeMask()
        {
            if (pool.Count > 0)
            {
                ComponentMask componentMask = pool.Dequeue();

                Array.Clear(componentMask.data, 0, componentMask.data.Length);
                componentMask.maxComponentIndex = -1;

                return componentMask;
            }

            return new ComponentMask
            {
                data = new int[16],
                maxComponentIndex = -1
            };
        }

        internal static unsafe ComponentMask PooledFromTypes(Type[] types, int count)
        {
            if (count == 0)
                return TakeMask();
            
            int* componentIndices = stackalloc int[count];
            for (int i = 0; i < count; i++)
            {
                var componentIndex = ComponentIndex.FromType(types[i]);
                componentIndices[i] = componentIndex;
            }
            return PooledFromIndices(componentIndices, count);
        }
        
        private static unsafe ComponentMask PooledFromIndices(int* indices, int count)
        {
            var mask = TakeMask();
            for (int i = 0; i < count; i++)
            {
                mask[indices[i]] = true;
            }

            return mask;
        }

        // private static ComponentMask FromIndices(int[] indices)
        // {
        //     var mask = TakeMask();
        //     foreach (var index in indices)
        //     {
        //         mask[index] = true;
        //     }
        //
        //     return mask;
        // }

        internal static ComponentMask Copy(ComponentMask mask)
        {
            var newMask = TakeMask();
            newMask.CopyFrom(mask);
            return newMask;
        }
        
        internal static ComponentMask Include(ComponentMask mask, int componentIndex)
        {
            var newMask = TakeMask();
            newMask.CopyFrom(mask);
            newMask[componentIndex] = true;
            return newMask;
        }
        
        internal static ComponentMask Exclude(ComponentMask mask, int componentIndex)
        {
            var newMask = TakeMask();
            newMask.CopyFrom(mask);
            newMask[componentIndex] = false;
            return newMask;
        }

        private void CopyFrom(ComponentMask other)
        {
            EnsureLength(other.data.Length);
            maxComponentIndex = other.maxComponentIndex;
            var dataSize = ToLength(other.maxComponentIndex + 1);
            for (int i = 0; i < dataSize; i++)
            {
                data[i] = other.data[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToLength(int componentCount)
        {
            return componentCount / BitsPerInt32 + 1;
        }

        public void Release()
        {
            if (pool.Count > MaxElementsInPool) return;
            pool.Enqueue(this);
        }

        public bool this[int index]
        {
            get
            {
                if (index > maxComponentIndex) return false;
                return (data[index / 32] & (1 << (index % 32))) != 0;
            }
            private set
            {
                if (value)
                {
                    if (index > maxComponentIndex)
                    {
                        maxComponentIndex = index;
                        EnsureComponents(maxComponentIndex + 1);
                    }
                    data[index / 32] |= (1 << (index % 32));
                }
                else
                {
                    if (index > maxComponentIndex) return;
                    var maxComponentIndexRecalculationRequired = index == maxComponentIndex;
                    data[index / 32] &= ~(1 << (index % 32));

                    if (maxComponentIndexRecalculationRequired)
                    {
                        maxComponentIndex = -1;
                        foreach (var componentIndex in this)
                        {
                            if (maxComponentIndex < componentIndex) maxComponentIndex = componentIndex;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureComponents(int componentCount)
        {
            var dataSize = ToLength(componentCount);
            if (data.Length < dataSize)
                Array.Resize(ref data, dataSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureLength(int dataSize)
        {
            if (data.Length < dataSize)
                Array.Resize(ref data, dataSize);
        }

        internal int[] GetComponentIndices()
        {
            var maxComponentIndex = BitsPerInt32 * data.Length;
            var list = new List<int>(maxComponentIndex + 1);
            for (int i = 0; i < maxComponentIndex; i++)
            {
                if (this[i]) list.Add(i);
            }

            return list.ToArray();
        }

        public IndexEnumerator GetEnumerator()
        {
            return new IndexEnumerator(this);
        }

        public bool Includes(ComponentMask includeMask)
        {
            if (includeMask.maxComponentIndex > maxComponentIndex) return false;
            
            var dataSize = ToLength(includeMask.maxComponentIndex + 1);
            for (int i = 0; i < dataSize; i++)
            {
                var im = includeMask.data[i];
                if ((data[i] & im) != im) return false;
            }

            return true;
        }
        
        public bool Excludes(ComponentMask excludeMask)
        {
            var m = Math.Min(excludeMask.maxComponentIndex, maxComponentIndex);
            var dataSize = ToLength(m + 1);
            for (int i = 0; i < dataSize; i++)
            {
                if ((data[i] & excludeMask.data[i]) != 0) return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetArrayLength(int n, int div)
        {
            return n > 0 ? (((n - 1) / div) + 1) : 0;
        }

        public bool Equals(ComponentMask other)
        {
            if (maxComponentIndex != other.maxComponentIndex) return false;
            if (maxComponentIndex < 0) return true;
            var dataSize = ToLength(maxComponentIndex + 1);
            for (var index = 0; index < dataSize; index++)
            {
                if (data[index] != other.data[index]) return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentMask other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (maxComponentIndex < 0) return 0;
            var dataSize = ToLength(maxComponentIndex + 1);
            var hashCode = dataSize;
            for (var index = 0; index < dataSize; index++)
            {
                hashCode = unchecked(hashCode * 314159 + data[index]);
            }

            return hashCode;
        }

        public void Dispose()
        {
            Release();
        }

        public struct IndexEnumerator : IEnumerator<int>
        {
            private ComponentMask mask;
            private int current;

            public IndexEnumerator(ComponentMask mask) : this()
            {
                this.mask = mask;
                current = -1;
            }

            public bool MoveNext()
            {
                while (++current <= mask.maxComponentIndex)
                {
                    if (mask[current]) break;
                }
                return current <= mask.maxComponentIndex;
            }

            public void Reset()
            {
                current = -1;
            }

            public int Current => current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                current = -1;
            }
        }
    }
}