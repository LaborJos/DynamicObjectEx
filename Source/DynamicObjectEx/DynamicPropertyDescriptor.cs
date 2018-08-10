namespace DynamicObjectEx
{
    using System;
    using System.ComponentModel;

    public class DynamicPropertyDescriptor : PropertyDescriptor
    {
        private readonly Type _componentType;
        private readonly Type _propertyType;
        
        public override Type ComponentType { get { return this._componentType; } }
        public override Type PropertyType { get { return this._propertyType; } }
        public override bool IsReadOnly { get { return false; } }

        public DynamicPropertyDescriptor(string name, Type componentType, Type propertyType) : base(name, null)
        {
            this._componentType = componentType;
            this._propertyType = propertyType;
        }

        public override bool CanResetValue(object component)
        {
            return true;
        }

        public override object GetValue(object component)
        {
            if (component is IDynamicObjectEx)
            {
                var result = default(object);

                if ((component as IDynamicObjectEx).GetMember(this.Name, out result))
                {
                    return result;
                }
            }

            throw new Exception();
        }

        public override void ResetValue(object component)
        {
            this.SetValue(this._componentType, null);
        }

        public override void SetValue(object component, object value)
        {
            if (component is IDynamicObjectEx)
            {
                if (!(component as IDynamicObjectEx).SetMember(this.Name, value))
                {
                    throw new Exception();
                }
            }
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }
}
