namespace Abg.Entities
{
    public readonly struct Entity
    {
        public readonly int Index;
        public readonly byte Version;

        internal Entity(int index, byte version)
        {
            Index = index;
            Version = version;
        }
    }
}