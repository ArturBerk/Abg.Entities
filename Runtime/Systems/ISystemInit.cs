namespace Abg.Entities
{
    public interface ISystemInit : ISystem
    {
        void OnInit(EntityWorld world);
    }
}