using System;
using System.Runtime.CompilerServices;

namespace Abg.Entities
{
    // public interface IEntityCollection
    // {
    //     int GetCount(bool includeDisabled = false);
    //     Entity GetEntity(int entityIndexInCollection);
    //     Entity GetEntity(int entityIndexInCollection);
    //     ComponentAccessor<T> GetComponents<T>();
    // }

    public sealed class EntityCollection
    {
        private readonly EntityWorld world;
        private readonly int startComponentIndex;
        private readonly int endComponentIndex;
        private readonly IComponentCollection[] components;
        private ArrayList<Entity> entities = new ArrayList<Entity>(128);
        private Bits bits;

        internal readonly ComponentMask ComponentMask;
        internal readonly int[] ComponentIndices;
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entities.Count - bits.DisabledCount;
        }

        internal IComponentCollection GetComponents(int componentIndex)
        {
            return components[componentIndex - startComponentIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount(bool includeDisabled = false)
        {
            return includeDisabled ? entities.Count : entities.Count - bits.DisabledCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetEntity(int entityIndexInCollection)
        {
            return entities[entityIndexInCollection];
        }

        internal ComponentCollection<T> GetComponentsInternal<T>()
        {
            return (ComponentCollection<T>)components[ComponentIndex<T>.Value - startComponentIndex];
        }

        public ComponentAccessor<T> GetComponents<T>()
        {
            return new ComponentAccessor<T>((ComponentCollection<T>)components[ComponentIndex<T>.Value - startComponentIndex]);
        }

        internal EntityCollection(ComponentMask componentMask)
        {
            this.ComponentMask = componentMask;
            endComponentIndex = 0;
            startComponentIndex = int.MaxValue;
            ComponentIndices = ComponentMask.GetComponentIndices();

            if (ComponentIndices.Length == 0)
            {
                startComponentIndex = -1;
                endComponentIndex = -1;
                components = Array.Empty<IComponentCollection>();
                return;
            }

            foreach (var index in ComponentIndices)
            {
                if (endComponentIndex < index)
                    endComponentIndex = index;
                if (startComponentIndex > index)
                    startComponentIndex = index;
            }

            components = new IComponentCollection[endComponentIndex - startComponentIndex + 1];
            foreach (var index in ComponentIndices)
            {
                components[index - startComponentIndex] = ComponentCollectionFactories.factories[index].Create();
            }

            bits = new Bits(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetEntityEnabled(int entityIndexInCollection, bool state)
        {
            bits[entityIndexInCollection] = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetEntityEnabled(int entityIndexInCollection)
        {
            return bits[entityIndexInCollection];
        }

        internal int AddEntity(Entity entity, bool enabled)
        {
            var index = entities.Count;
            entities.Add(entity);
            bits[index] = enabled;

            foreach (var componentIndex in ComponentIndices)
            {
                components[componentIndex - startComponentIndex].Add();
            }

            return index;
        }

        internal void RemoveEntity(int entityIndexInCollection)
        {
            if (entityIndexInCollection < entities.Count - 1)
            {
                bits[entityIndexInCollection] = bits[entities.Count - 1];
                bits[entities.Count - 1] = true;
            }
            entities.UnorderedRemoveAt(entityIndexInCollection);
            foreach (var componentIndex in ComponentIndices)
            {
                components[componentIndex - startComponentIndex].RemoveAt(entityIndexInCollection);
            }
        }
    }
}