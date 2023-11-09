using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Abg.Entities
{
    public sealed class EntityWorld
    {
        private Dictionary<Type, Entities> entityGroups = new Dictionary<Type, Entities>();

        private Dictionary<ComponentMask, EntityCollection> entityCollections =
            new Dictionary<ComponentMask, EntityCollection>();

        private ArrayList<EntityContainer> entities = new ArrayList<EntityContainer>(128);
        private Queue<int> freeEntities = new Queue<int>(32);
        private EntityBuilder builder;
        internal ushort version;

        internal IEnumerable<EntityCollection> Collections => entityCollections.Values;

        public T GetEntityGroup<T>() where T : Entities
        {
            if (entityGroups.TryGetValue(typeof(T), out var group)) return (T)group;

            group = (Entities)Activator.CreateInstance(typeof(T), this);
            entityGroups.Add(typeof(T), group);

            return (T)group;
        }

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
            entityContainer.CollectionIndex = entityCollection.AddEntity(entity, true);

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
        
        public void SetEntityEnabled(Entity entity, bool state)
        {
            CheckEntity(entity);
            ref var container = ref entities[entity.Index];
            container.Collection.SetEntityEnabled(container.CollectionIndex, state);
        }

        public bool GetEntityEnabled(Entity entity)
        {
            CheckEntity(entity);
            ref var container = ref entities[entity.Index];
            return container.Collection.GetEntityEnabled(container.CollectionIndex);
        }

        public void SetComponent<T>(Entity entity, T component = default)
        {
            CheckEntity(entity);
            ref var container = ref entities[entity.Index];

            var newComponentIndex = ComponentCollectionFactories<T>.ComponentIndex;
            var sourceCollection = container.Collection;
            if (container.Collection.ComponentMask[newComponentIndex])
            {
                sourceCollection.GetComponentsInternal<T>().Set(container.CollectionIndex, component);
                return;
            }

            using var destMask = ComponentMask.Include(container.Collection.ComponentMask, newComponentIndex);
            var destCollection = GetCollectionFromMask(destMask);

            CopyEntity(entity, ref container, sourceCollection, destCollection);
            destCollection.GetComponentsInternal<T>().Set(container.CollectionIndex, component);
        }

        private void CopyEntity(Entity entity, ref EntityContainer container,
            EntityCollection sourceCollection, EntityCollection destCollection)
        {
            var indexInSourceCollection = container.CollectionIndex;
            container.Collection = destCollection;
            var state = sourceCollection.GetEntityEnabled(indexInSourceCollection);
            container.CollectionIndex = destCollection.AddEntity(entity, state);
            foreach (var componentIndex in sourceCollection.ComponentIndices)
            {
                if (destCollection.ComponentMask[componentIndex])
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
                .GetComponentsInternal<T>()
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
            public EntityBuilder With<T>()
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