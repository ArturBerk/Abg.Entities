using System;

namespace Abg.Entities
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class IncludeDisabledAttribute : Attribute
    {
    }
}