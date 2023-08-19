namespace Abg.Entities
{
    public readonly struct TimeData
    {
        public readonly float Elapsed;
        public readonly float Delta;

        public TimeData(float elapsed, float delta)
        {
            Elapsed = elapsed;
            Delta = delta;
        }
    }
}