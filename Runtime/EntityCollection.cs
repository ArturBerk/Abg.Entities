using System;

namespace Abg.Entities
{
    public interface IEntityCollection
    {
        int Count { get; }
        Entity GetEntity(int entityIndexInCollection);
        ComponentAccessor<T> GetComponents<T>();
    }

    internal class EntityCollection : IEntityCollection
    {
        private readonly EntityWorld world;
        private readonly int startComponentIndex;
        private readonly int endComponentIndex;
        private readonly IComponentCollection[] components;
        private ArrayList<Entity> entities = new ArrayList<Entity>(128);

        public readonly ComponentMask ComponentMask;
        public readonly int[] ComponentIndices;
        public int Count => entities.Count;
        
        internal IComponentCollection GetComponents(int componentIndex)
        {
            return components[componentIndex - startComponentIndex];
        }

        public Entity GetEntity(int entityIndexInCollection)
        {
            return entities[entityIndexInCollection];
        }

        internal ComponentCollection<T> GetComponents<T>()
        {
            return (ComponentCollection<T>)components[ComponentIndex<T>.Value - startComponentIndex];
        }

        ComponentAccessor<T> IEntityCollection.GetComponents<T>()
        {
            return new ComponentAccessor<T>((ComponentCollection<T>)components[ComponentIndex<T>.Value - startComponentIndex]);
        }

        public EntityCollection(ComponentMask componentMask)
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
        }

        public int AddEntity(Entity entity)
        {
            var index = entities.Count;
            entities.Add(entity);

            foreach (var componentIndex in ComponentIndices)
            {
                components[componentIndex - startComponentIndex].Add();
            }

            return index;
        }

        public void RemoveEntity(int entityIndexInCollection)
        {
            entities.UnorderedRemoveAt(entityIndexInCollection);
            foreach (var componentIndex in ComponentIndices)
            {
                components[componentIndex - startComponentIndex].RemoveAt(entityIndexInCollection);
            }
        }
    }
}