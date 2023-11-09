using System;
using System.Runtime.CompilerServices;

namespace Abg.Entities
{
    internal struct Bits
    {
        private const int BitsPerInt32 = 32;
        private const int MaxElementsInPool = 8;

        private int[] data;
        private int disabledCount;

        public Bits(int capacity)
        {
            data = new int[capacity];
            disabledCount = 0;
        }

        public bool this[int index]
        {
            get
            {
                if (index > data.Length * BitsPerInt32) return true;
                return (data[index / BitsPerInt32] & (1 << (index % BitsPerInt32))) != 0;
            }
            set
            {
                if (this[index] == value) return;
                var dataIndex = index / BitsPerInt32;
                EnsureCapacity(dataIndex + 1);
                if (value)
                {
                    data[dataIndex] |= 1 << (index % BitsPerInt32);
                    --disabledCount;
                }
                else
                {
                    data[dataIndex] &= ~(1 << (index % BitsPerInt32));
                    ++disabledCount;
                }
            }
        }

        public int DisabledCount => disabledCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int length)
        {
            if (length > data.Length)
            {
                var newLength = (int)(length * 1.5f);
                if (newLength < 128) newLength = 128;
                Array.Resize(ref data, newLength);
            }
        }
    }
}