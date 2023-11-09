using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Abg.Entities
{
    public class Entities
    {
        private EntityWorld world;
        private readonly ComponentMask includeMask;
        private readonly ComponentMask excludeMask;

        private EntityCollection[] collections = Array.Empty<EntityCollection>();
        private ushort collectionsVersion = 0;

        public Entities(EntityWorld world, Type[] includeTypes, Type[] excludeTypes)
        {
            this.world = world;
            includeMask = ComponentMask.PooledFromTypes(includeTypes, includeTypes.Length);
            excludeMask = ComponentMask.PooledFromTypes(excludeTypes, excludeTypes.Length);
        }

        public int GetCount(bool includeDisabled = false)
        {
            Invalidate();
            var count = 0;
            foreach (EntityCollection collection in collections)
            {
                count += collection.GetCount(includeDisabled);
            }

            return count;
        }

        public IReadOnlyList<EntityCollection> PrepareCollections()
        {
            Invalidate();

            return collections;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Invalidate()
        {
            if (collectionsVersion == world.version) return;
            collections = world.Collections
                .Where(c => c.ComponentMask.Includes(includeMask) && c.ComponentMask.Excludes(excludeMask))
                .ToArray();
            collectionsVersion = world.version;
        }
    }
}