namespace DynamicObjectEx
{
    using System;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DynamicObjectExIgnoreAttribute : Attribute
    {
    }
}
