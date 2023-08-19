using System;
using System.Collections.Generic;

namespace Abg.Entities
{
    internal static class ComponentIndex
    {
        private static object syncRoot = new object();
        private static int nextIndex = 0;
        private static Dictionary<Type, int> typeToIndex = new Dictionary<Type, int>();
        
        public static int FromType(Type type)
        {
            if (typeToIndex.TryGetValue(type, out var index)) return index;
            lock (syncRoot)
            {
                index = nextIndex++;
                var newTypeToIndex = new Dictionary<Type, int>(typeToIndex);
                newTypeToIndex.Add(type, index);
                typeToIndex = newTypeToIndex;
            }

            return index;
        }
    }

    internal static class ComponentIndex<T>
    {
        public static readonly int Value;

        static ComponentIndex()
        {
            Value = ComponentIndex.FromType(typeof(T));
        }
    }

    // internal static class ComponentType
    // {
    //     internal static int GlobalIndex = -1;
    // }
    //
    // internal static class ComponentType<T>
    // {
    //     public static readonly int Index;
    //
    //     static ComponentType()
    //     {
    //         Index = Interlocked.Increment(ref ComponentType.GlobalIndex);
    //     }
    // }
}