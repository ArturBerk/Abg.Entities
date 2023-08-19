using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Abg.Entities
{
    public sealed class EntityWorld
    {
        private Dictionary<ComponentMask, EntityCollection> entityCollections =
            new Dictionary<ComponentMask, EntityCollection>();

        private ArrayList<EntityContainer> entities = new ArrayList<EntityContainer>(128);
        private Queue<int> freeEntities = new Queue<int>(32);
        private EntityBuilder builder;
        internal ushort version;

        internal IEnumerable<EntityCollection> Collections => entityCollections.Values;

        public EntityWorld()
        {
            var emptyArchetype = new EntityCollection(ComponentMask.Empty);
            entityCollections[emptyArchetype.ComponentMask] = emptyArchetype;
            builder = new EntityBuilder(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EntityCollection GetCollectionFromMask(ComponentMask mask)
        {
            if (!entityCollections.TryGetValue(mask, out var entityCollection))
            {
                var newMask = ComponentMask.Copy(mask);
                entityCollection = new EntityCollection(newMask);
                entityCollections.Add(newMask, entityCollection);
                unchecked
                {
                    version++;
                }
            }

            return entityCollection;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityCollection GetCollectionFromTypes(params Type[] componentTypes)
        {
            using ComponentMask mask = ComponentMask.PooledFromTypes(componentTypes, componentTypes.Length);
            return GetCollectionFromMask(mask);
        }

        public EntityBuilder NewEntity => builder;

        private Entity CreateEntity(ArrayList<Type> types)
        {
            using ComponentMask mask = ComponentMask.PooledFromTypes(types.Buffer, types.Count);
            var entityCollection = GetCollectionFromMask(mask);

            var entityIndex = default(int);
            if (freeEntities.Count > 0)
            {
                entityIndex = freeEntities.Dequeue();
            }
            else
            {
                entityIndex = entities.Count();
                entities.Add(new EntityContainer());
            }

            ref EntityContainer entityContainer = ref entities[entityIndex];
            var entity = new Entity(entityIndex, entityContainer.Version);
            entityContainer.Collection = entityCollection;
            entityContainer.CollectionIndex = entityCollection.AddEntity(entity);

            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            CheckEntity(entity);

            ref var container = ref entities[entity.Index];
            container.Collection.RemoveEntity(container.CollectionIndex);
            container.Version = unchecked(++container.Version);

            freeEntities.Enqueue(entity.Index);
        }

        public void SetComponent<T>(Entity entity, T component = default)
        {
            CheckEntity(entity);
            ref var container = ref entities[entity.Index];

            var newComponentIndex = ComponentCollectionFactories<T>.ComponentIndex;
            var sourceCollection = container.Collection;
            if (container.Collection.ComponentMask[newComponentIndex])
            {
                sourceCollection.GetComponents<T>().Set(container.CollectionIndex, component);
                return;
            }

            using var destMask = ComponentMask.Include(container.Collection.ComponentMask, newComponentIndex);
            var destCollection = GetCollectionFromMask(destMask);

            CopyEntity(entity, ref container, sourceCollection, destCollection);
            destCollection.GetComponents<T>().Set(container.CollectionIndex, component);
        }

        private void CopyEntity(Entity entity, ref EntityContainer container,
            EntityCollection sourceCollection, EntityCollection destCollection)
        {
            var indexInSourceCollection = container.CollectionIndex;
            container.Collection = destCollection;
            container.CollectionIndex = destCollection.AddEntity(entity);
            foreach (var componentIndex in sourceCollection.ComponentIndices)
            {
                sourceCollection.GetComponents(componentIndex).CopyTo(indexInSourceCollection,
                    destCollection.GetComponents(componentIndex), container.CollectionIndex);
            }

            sourceCollection.RemoveEntity(indexInSourceCollection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckEntity(Entity entity)
        {
            if (entities.Count <= entity.Index || entities[entity.Index].Version != entity.Version)
                throw new Exception("Entity not found");
        }
        
        public bool Exists(Entity entity)
        {
            return entities.Count > entity.Index && entities[entity.Index].Version == entity.Version;
        }

        public bool HasComponent<T>(Entity entity)
        {
            CheckEntity(entity);
            ref var container = ref entities[entity.Index];
            return container.Collection.ComponentMask[ComponentIndex<T>.Value];
        }

        public ref T GetComponent<T>(Entity entity)
        {
            CheckEntity(entity);
            ref var container = ref entities[entity.Index];
            return ref container.Collection
                .GetComponents<T>()
                .GetComponent(container.CollectionIndex);
        }

        public bool RemoveComponent<T>(Entity entity)
        {
            CheckEntity(entity);
            ref var container = ref entities[entity.Index];

            var removeComponentIndex = ComponentCollectionFactories<T>.ComponentIndex;
            var sourceCollection = container.Collection;
            if (!container.Collection.ComponentMask[removeComponentIndex])
            {
                return false;
            }

            using var destMask = ComponentMask.Exclude(container.Collection.ComponentMask, removeComponentIndex);
            var destCollection = GetCollectionFromMask(destMask);

            CopyEntity(entity, ref container, sourceCollection, destCollection);
            return true;
        }

        private struct EntityContainer
        {
            public EntityCollection Collection;
            public int CollectionIndex;
            public byte Version;
        }

        public class EntityBuilder
        {
            private readonly EntityWorld world;
            private ArrayList<Type> types;

            public EntityBuilder(EntityWorld world)
            {
                this.world = world;
                types = new ArrayList<Type>(8);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EntityBuilder WithComponent<T>()
            {
                types.Add(ComponentCollectionFactories<T>.InitializeType);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Entity Create()
            {
                Entity entity = world.CreateEntity(types);
                types.Clear();
                return entity;
            }
        }
    }
}