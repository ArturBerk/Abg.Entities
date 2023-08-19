namespace Abg.Entities
{
    public interface ISystemDestroy : ISystem
    {
        void OnDestroy(EntityWorld world);
    }
}