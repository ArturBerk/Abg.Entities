using System;

namespace Abg.Entities
{
    internal interface IComponentCollectionFactory
    {
        IComponentCollection Create();
    }

    internal static class ComponentCollectionFactories<T>
    {
        public static Type InitializeType => typeof(T);

        public static int ComponentIndex => ComponentIndex<T>.Value;

        static ComponentCollectionFactories()
        {
            var typeIndex = ComponentIndex<T>.Value;
            if (ComponentCollectionFactories.factories == null)
            {
                ComponentCollectionFactories.factories = new IComponentCollectionFactory[typeIndex + 1];
            }
            else if (typeIndex >= ComponentCollectionFactories.factories.Length)
            {
                Array.Resize(ref ComponentCollectionFactories.factories, typeIndex + 1);
            }

            ComponentCollectionFactories.factories[typeIndex] = new ComponentCollectionFactory(); 
        }

        private class ComponentCollectionFactory : IComponentCollectionFactory
        {
            public IComponentCollection Create()
            {
                return new ComponentCollection<T>();
            }
        }
    }
    
    internal static class ComponentCollectionFactories
    {
        internal static IComponentCollectionFactory[] factories;
    }
}