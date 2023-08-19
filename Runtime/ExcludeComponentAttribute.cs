using System;

namespace Abg.Entities
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ExcludeComponentAttribute : Attribute
    {
        public readonly Type Type;

        public ExcludeComponentAttribute(Type type)
        {
            Type = type;
        }
    }
}