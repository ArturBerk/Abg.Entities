using System.Runtime.CompilerServices;

namespace Abg.Entities
{
    public readonly struct ComponentRef<T>
    {
        private readonly ComponentAccessor<T> components;
        private readonly int componentIndex;

        public ComponentRef(ComponentAccessor<T> components, int componentIndex)
        {
            this.components = components;
            this.componentIndex = componentIndex;
        }

        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref components[componentIndex];
        }
    }
}