namespace Abg.Entities
{
    public interface ISystemTick : ISystem
    {
        void OnTick(TimeData time);
    }
}