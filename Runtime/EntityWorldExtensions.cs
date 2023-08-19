using System.Runtime.CompilerServices;

namespace Abg.Entities
{
    public static class EntityWorldExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityWorld.EntityBuilder WithComponents<T1, T2>(this EntityWorld.EntityBuilder builder)
        {
            return builder.WithComponent<T1>().WithComponent<T2>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityWorld.EntityBuilder WithComponents<T1, T2, T3>(this EntityWorld.EntityBuilder builder)
        {
            return builder.WithComponent<T1>().WithComponent<T2>().WithComponent<T3>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityWorld.EntityBuilder WithComponents<T1, T2, T3, T4>(this EntityWorld.EntityBuilder builder)
        {
            return builder.WithComponent<T1>().WithComponent<T2>().WithComponent<T3>().WithComponent<T4>();
        }
    }
}