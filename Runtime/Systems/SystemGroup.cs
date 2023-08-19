using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Abg.Entities
{
    public sealed class SystemGroup : ISystem, ISystemTick, ISystemInit, ISystemDestroy, IReadOnlyList<ISystem>
    {
        private readonly ISystem[] systems;
        private readonly ISystemTick[] tickSystems;

        public int Count => systems.Length;
        public ISystem this[int index] => systems[index];

        public SystemGroup(IEnumerable<ISystem> systems)
        {
            this.systems = systems.ToArray();
            tickSystems = this.systems.OfType<ISystemTick>().ToArray();
        }

        public SystemGroup(params ISystem[] systems)
        {
            this.systems = systems;
            tickSystems = this.systems.OfType<ISystemTick>().ToArray();
        }

        public void OnTick(TimeData time)
        {
            foreach (ISystemTick system in tickSystems)
            {
                system.OnTick(time);
            }
        }

        public void OnInit(EntityWorld world)
        {
            foreach (ISystem system in systems)
            {
                if (system is not ISystemInit initSystem) continue;
                initSystem.OnInit(world);
            }
        }

        public void OnDestroy(EntityWorld world)
        {
            foreach (ISystem system in systems)
            {
                if (system is not ISystemDestroy destroySystem) continue;
                destroySystem.OnDestroy(world);
            }
        }

        public IEnumerator<ISystem> GetEnumerator()
        {
            return ((IEnumerable<ISystem>)systems).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ISystem>)systems).GetEnumerator();
        }
    }
}