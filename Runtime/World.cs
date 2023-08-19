using System;

namespace Abg.Entities
{
    public readonly struct World : IDisposable
    {
        public readonly EntityWorld Entities;
        public readonly SystemsRoot Systems;
        
        public World(SystemsRootBuilder builder)
        {
            Entities = new EntityWorld();
            Systems = builder.Build(Entities);
        }

        public void Dispose()
        {
            Systems?.Dispose();
        }
    }
}