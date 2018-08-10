namespace DynamicObjectEx
{
    using System;

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DynamicObjectExConverterAttribute : Attribute
    {
        public Type ConvertType { get { return this.Converter.ConvertType; } }

        public IDynamicObjectExConverter Converter { get; private set; }

        public DynamicObjectExConverterAttribute(Type converter)
        {
            var instance = converter.CreateInstance<IDynamicObjectExConverter>();

            if (instance != null)
            {
                this.Converter = instance;
            }
            else
            {
                throw new ArgumentException("converter", "Convert는 null이 아니고 IDynamicObjectExConverter 형식만 사용가능 합니다");
            }
        }
    }
}
