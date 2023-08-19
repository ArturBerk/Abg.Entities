using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Abg.Entities
{
    public class SystemsRoot : IEnumerable<ISystem>, IDisposable
    {
        public static SystemsRootBuilder Builder => new SystemsRootBuilder();
        
        private readonly EntityWorld world;
        private readonly SystemGroup earlyUpdate;
        private readonly SystemGroup update;
        private readonly SystemGroup lateUpdate;
        private readonly SystemGroup fixedUpdate;

        public SystemGroup EarlyUpdate => earlyUpdate;
        public SystemGroup Update => update;
        public SystemGroup LateUpdate => lateUpdate;
        public SystemGroup FixedUpdate => fixedUpdate;


        internal SystemsRoot(EntityWorld world,
            SystemGroup earlyUpdate,
            SystemGroup update,
            SystemGroup lateUpdate,
            SystemGroup fixedUpdate)
        {
            this.world = world;
            this.earlyUpdate = earlyUpdate;
            this.update = update;
            this.lateUpdate = lateUpdate;
            this.fixedUpdate = fixedUpdate;

            earlyUpdate?.OnInit(this.world);
            update?.OnInit(this.world);
            lateUpdate?.OnInit(this.world);
            fixedUpdate?.OnInit(this.world);

            PlayerLoopExtensions.ModifyCurrentPlayerLoop((ref PlayerLoopSystem system) =>
            {
                if (earlyUpdate != null)
                {
                    system.GetSystem<EarlyUpdate>().AddSystem<SystemsRoot>(OnEarlyUpdate);
                }

                if (update != null)
                {
                    system.GetSystem<Update>().AddSystem<SystemsRoot>(OnUpdate);
                }

                if (lateUpdate != null)
                {
                    system.GetSystem<PreLateUpdate>().AddSystem<SystemsRoot>(OnLateUpdate);
                }

                if (lateUpdate != null)
                {
                    system.GetSystem<FixedUpdate>().AddSystem<SystemsRoot>(OnFixedUpdate);
                }
            });
        }

        private void OnEarlyUpdate()
        {
            earlyUpdate.OnTick(new TimeData(Time.time, Time.smoothDeltaTime));
        }

        private void OnUpdate()
        {
            update.OnTick(new TimeData(Time.time, Time.smoothDeltaTime));
        }

        private void OnLateUpdate()
        {
            lateUpdate.OnTick(new TimeData(Time.time, Time.smoothDeltaTime));
        }

        private void OnFixedUpdate()
        {
            fixedUpdate.OnTick(new TimeData(Time.fixedTime, Time.fixedDeltaTime));
        }

        public void Dispose()
        {
            earlyUpdate?.OnDestroy(world);
            update?.OnDestroy(world);
            lateUpdate?.OnDestroy(world);
            fixedUpdate?.OnDestroy(world);

            PlayerLoopExtensions.ModifyCurrentPlayerLoop((ref PlayerLoopSystem system) =>
            {
                if (earlyUpdate != null)
                {
                    system.GetSystem<EarlyUpdate>().RemoveSystem<SystemsRoot>(false);
                }

                if (update != null)
                {
                    system.GetSystem<Update>().RemoveSystem<SystemsRoot>(false);
                }

                if (lateUpdate != null)
                {
                    system.GetSystem<PreLateUpdate>().RemoveSystem<SystemsRoot>(false);
                }

                if (lateUpdate != null)
                {
                    system.GetSystem<FixedUpdate>().RemoveSystem<SystemsRoot>(false);
                }
            });
        }

        private IEnumerable<ISystem> GetSystemsRecursive(IEnumerable<ISystem> systemGroup)
        {
            foreach (ISystem system in systemGroup)
            {
                if (system is IEnumerable<ISystem> subSystemGroup)
                {
                    foreach (ISystem system1 in GetSystemsRecursive(subSystemGroup))
                    {
                        yield return system1;
                    }
                }
                else
                {
                    yield return system;
                }
            }
        }
        
        public IEnumerator<ISystem> GetEnumerator()
        {
            if (earlyUpdate != null)
            {
                foreach (ISystem system in GetSystemsRecursive(earlyUpdate))
                {
                    yield return system;
                }
            }
            if (update != null)
            {
                foreach (ISystem system in GetSystemsRecursive(update))
                {
                    yield return system;
                }
            }
            if (lateUpdate != null)
            {
                foreach (ISystem system in GetSystemsRecursive(lateUpdate))
                {
                    yield return system;
                }
            }
            if (fixedUpdate != null)
            {
                foreach (ISystem system in GetSystemsRecursive(fixedUpdate))
                {
                    yield return system;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class SystemsRootBuilder
    {
        private SystemGroupBuilder earlyUpdateSystems;
        private SystemGroupBuilder updateSystems;
        private SystemGroupBuilder lateUpdateSystems;
        private SystemGroupBuilder fixedUpdateSystems;

        public SystemsRoot Build(EntityWorld world) => new SystemsRoot(world,
            earlyUpdateSystems?.Build(),
            updateSystems?.Build(),
            lateUpdateSystems?.Build(),
            fixedUpdateSystems?.Build()
        );

        public SystemsRootBuilder OnEarlyUpdate(Action<SystemGroupBuilder> build)
        {
            earlyUpdateSystems = new SystemGroupBuilder();
            build(earlyUpdateSystems);
            return this;
        }
        
        public SystemsRootBuilder OnUpdate(Action<SystemGroupBuilder> build)
        {
            updateSystems = new SystemGroupBuilder();
            build(updateSystems);
            return this;
        }
        
        public SystemsRootBuilder OnLateUpdate(Action<SystemGroupBuilder> build)
        {
            lateUpdateSystems = new SystemGroupBuilder();
            build(lateUpdateSystems);
            return this;
        }
        
        public SystemsRootBuilder OnFixedUpdate(Action<SystemGroupBuilder> build)
        {
            fixedUpdateSystems = new SystemGroupBuilder();
            build(fixedUpdateSystems);
            return this;
        }
    }

    public class SystemGroupBuilder
    {
        private readonly List<object> systemsOrGroups = new List<object>();

        internal SystemGroup Build()
        {
            return new SystemGroup(systemsOrGroups.Select(o => o is SystemGroupBuilder builder 
                ? builder.Build() 
                : (ISystem)o));
        }

        public SystemGroupBuilder Group(Action<SystemGroupBuilder> build)
        {
            var builder = new SystemGroupBuilder();
            build(builder);
            systemsOrGroups.Add(builder);
            return this;
        }

        public SystemGroupBuilder Execute<T>() where T : ISystem, new()
        {
            systemsOrGroups.Add(new T());
            return this;
        }

        public SystemGroupBuilder Execute(ISystem system)
        {
            systemsOrGroups.Add(system);
            return this;
        }
    }
}