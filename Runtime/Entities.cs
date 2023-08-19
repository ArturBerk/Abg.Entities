using System;
using System.Collections.Generic;
using System.Linq;

namespace Abg.Entities
{
    public class Entities
    {
        private EntityWorld world;
        private readonly ComponentMask includeMask;
        private readonly ComponentMask excludeMask;

        private IEntityCollection[] collections = Array.Empty<IEntityCollection>();
        private ushort collectionsVersion = 0;

        public Entities(EntityWorld world, Type[] includeTypes, Type[] excludeTypes)
        {
            this.world = world;
            includeMask = ComponentMask.PooledFromTypes(includeTypes, includeTypes.Length);
            excludeMask = ComponentMask.PooledFromTypes(excludeTypes, excludeTypes.Length);
        }

        public IReadOnlyList<IEntityCollection> PrepareCollections()
        {
            if (collectionsVersion != world.version)
            {
                collections = world.Collections
                    .Where(c => c.ComponentMask.Includes(includeMask) && c.ComponentMask.Excludes(excludeMask))
                    .Cast<IEntityCollection>()
                    .ToArray();
                collectionsVersion = world.version;
            }

            return collections;
        }
    }
}