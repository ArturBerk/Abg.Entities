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

        private List<SystemGroup> earlyUpdateGroups;
        private List<SystemGroup> updateGroups;
        private List<SystemGroup> lateUpdateGroups;
        private List<SystemGroup> fixedUpdateGroups;

        private readonly EntityWorld world;

        internal SystemsRoot(EntityWorld world)
        {
            this.world = world;
        }

        public void Start()
        {
            if (earlyUpdateGroups != null)
            {
                foreach (SystemGroup group in earlyUpdateGroups)
                {
                    group.OnInit(world);
                }
            }

            if (updateGroups != null)
            {
                foreach (SystemGroup group in updateGroups)
                {
                    group.OnInit(world);
                }
            }

            if (lateUpdateGroups != null)
            {
                foreach (SystemGroup group in lateUpdateGroups)
                {
                    group.OnInit(world);
                }
            }

            if (fixedUpdateGroups != null)
            {
                foreach (SystemGroup group in fixedUpdateGroups)
                {
                    group.OnInit(world);
                }
            }

            PlayerLoopExtensions.ModifyCurrentPlayerLoop((ref PlayerLoopSystem system) =>
            {
                if (earlyUpdateGroups != null)
                {
                    system.GetSystem<EarlyUpdate>().AddSystem<SystemsRoot>(OnEarlyUpdate);
                }

                if (updateGroups != null)
                {
                    system.GetSystem<Update>().AddSystem<SystemsRoot>(OnUpdate);
                }

                if (lateUpdateGroups != null)
                {
                    system.GetSystem<PreLateUpdate>().AddSystem<SystemsRoot>(OnLateUpdate);
                }

                if (fixedUpdateGroups != null)
                {
                    system.GetSystem<FixedUpdate>().AddSystem<SystemsRoot>(OnFixedUpdate);
                }
            });
        }

        internal void AddEarlyUpdateGroup(SystemGroup system)
        {
            if (updateGroups == null) updateGroups = new List<SystemGroup>(4);
            updateGroups.Add(system);
        }

        internal void AddUpdateGroup(SystemGroup system)
        {
            if (updateGroups == null) updateGroups = new List<SystemGroup>(4);
            updateGroups.Add(system);
        }

        internal void AddLateUpdateGroup(SystemGroup system)
        {
            if (lateUpdateGroups == null) lateUpdateGroups = new List<SystemGroup>(4);
            lateUpdateGroups.Add(system);
        }

        internal void AddFixedUpdateGroup(SystemGroup system)
        {
            if (fixedUpdateGroups == null) fixedUpdateGroups = new List<SystemGroup>(4);
            fixedUpdateGroups.Add(system);
        }

        private void OnEarlyUpdate()
        {
            foreach (SystemGroup group in earlyUpdateGroups)
            {
                group.OnTick(new TimeData(Time.time, Time.smoothDeltaTime));
            }
        }

        private void OnUpdate()
        {
            foreach (SystemGroup group in updateGroups)
            {
                group.OnTick(new TimeData(Time.time, Time.smoothDeltaTime));
            }
        }

        private void OnLateUpdate()
        {
            foreach (SystemGroup group in lateUpdateGroups)
            {
                group.OnTick(new TimeData(Time.time, Time.smoothDeltaTime));
            }
        }

        private void OnFixedUpdate()
        {
            foreach (SystemGroup group in fixedUpdateGroups)
            {
                group.OnTick(new TimeData(Time.time, Time.fixedDeltaTime));
            }
        }

        public void Stop()
        {
            PlayerLoopExtensions.ModifyCurrentPlayerLoop((ref PlayerLoopSystem system) =>
            {
                system.GetSystem<EarlyUpdate>().RemoveSystem<SystemsRoot>(false);
                system.GetSystem<Update>().RemoveSystem<SystemsRoot>(false);
                system.GetSystem<PreLateUpdate>().RemoveSystem<SystemsRoot>(false);
                system.GetSystem<FixedUpdate>().RemoveSystem<SystemsRoot>(false);
            });
        }

        public void Dispose()
        {
            Stop();
            
            if (earlyUpdateGroups != null)
            {
                foreach (SystemGroup group in earlyUpdateGroups)
                {
                    group.OnDestroy(world);
                }
            }

            if (updateGroups != null)
            {
                foreach (SystemGroup group in updateGroups)
                {
                    group.OnDestroy(world);
                }
            }

            if (lateUpdateGroups != null)
            {
                foreach (SystemGroup group in lateUpdateGroups)
                {
                    group.OnDestroy(world);
                }
            }

            if (fixedUpdateGroups != null)
            {
                foreach (SystemGroup group in fixedUpdateGroups)
                {
                    group.OnDestroy(world);
                }
            }
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
            if (earlyUpdateGroups != null)
            {
                foreach (SystemGroup group in earlyUpdateGroups)
                {
                    foreach (ISystem system in GetSystemsRecursive(group))
                    {
                        yield return system;
                    }
                }
            }

            if (updateGroups != null)
            {
                foreach (SystemGroup group in updateGroups)
                {
                    foreach (ISystem system in GetSystemsRecursive(group))
                    {
                        yield return system;
                    }
                }
            }

            if (lateUpdateGroups != null)
            {
                foreach (SystemGroup group in lateUpdateGroups)
                {
                    foreach (ISystem system in GetSystemsRecursive(group))
                    {
                        yield return system;
                    }
                }
            }

            if (fixedUpdateGroups != null)
            {
                foreach (SystemGroup group in fixedUpdateGroups)
                {
                    foreach (ISystem system in GetSystemsRecursive(group))
                    {
                        yield return system;
                    }
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
        private List<(SystemGroupBuilder, SystemGroupType)> builders = new List<(SystemGroupBuilder, SystemGroupType)>();

        public SystemsRoot Build(EntityWorld world)
        {
            var systemsRoot = new SystemsRoot(world);

            foreach (var (builder, type) in builders)
            {
                switch (type)
                {
                    case SystemGroupType.Update:
                        systemsRoot.AddUpdateGroup(builder.Build());
                        break;
                    case SystemGroupType.EarlyUpdate:
                        systemsRoot.AddEarlyUpdateGroup(builder.Build());
                        break;
                    case SystemGroupType.LateUpdate:
                        systemsRoot.AddLateUpdateGroup(builder.Build());
                        break;
                    case SystemGroupType.FixedUpdate:
                        systemsRoot.AddFixedUpdateGroup(builder.Build());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            return systemsRoot;
        }

        public SystemsRootBuilder OnEarlyUpdate(Action<SystemGroupBuilder> build)
        {
            var builder = new SystemGroupBuilder();
            builders.Add((builder, SystemGroupType.EarlyUpdate));
            build(builder);
            return this;
        }

        public SystemsRootBuilder OnUpdate(Action<SystemGroupBuilder> build)
        {
            var builder = new SystemGroupBuilder();
            builders.Add((builder, SystemGroupType.Update));
            build(builder);
            return this;
        }

        public SystemsRootBuilder OnLateUpdate(Action<SystemGroupBuilder> build)
        {
            var builder = new SystemGroupBuilder();
            builders.Add((builder, SystemGroupType.LateUpdate));
            build(builder);
            return this;
        }

        public SystemsRootBuilder OnFixedUpdate(Action<SystemGroupBuilder> build)
        {
            var builder = new SystemGroupBuilder();
            builders.Add((builder, SystemGroupType.FixedUpdate));
            build(builder);
            return this;
        }

        private enum SystemGroupType
        {
            Update = 0,
            EarlyUpdate = 1,
            LateUpdate = 2,
            FixedUpdate = 3,
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