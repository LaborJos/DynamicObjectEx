using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace DynamicObjectEx
{
    public interface IDynamicObjectEx : IDisposable
    {
        bool IsConverted { get; }
        IDynamicObjectExConverter Converter { get; }

        Type GetOriginalType();

        bool GetMember(string name, out object result);

        bool SetMember(string name, object value);

        dynamic AsDynamic();
    }

    public class DynamicObjectEx<T> : DynamicObject, IDynamicObjectEx, ICustomTypeDescriptor, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private bool _disposed = false;

        private readonly Dictionary<string, object> _dictionary;

        public bool IsConverted { get { return this.Converter != null; } }
        public IDynamicObjectExConverter Converter { get; protected set; }

        public DynamicObjectEx()
        {
            this._dictionary = new Dictionary<string, object>();
        }

        public DynamicObjectEx(IDynamicObjectExConverter converter) : this()
        {
            this.Converter = converter;
        }

        ~DynamicObjectEx()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return this.GetMember(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return this.SetMember(binder.Name, value);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this._dictionary.Keys;
        }

        public Type GetOriginalType()
        {
            return typeof(T);
        }

        public dynamic AsDynamic()
        {
            return this as dynamic;
        }

        public void Map(T destination)
        {
            this.Map<T>(destination);
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose(bool disposing)
        {
            if (this._disposed) return;

            if (disposing)
            {
                this._dictionary.Clear();
            }

            this._disposed = true;
        }

        public bool GetMember(string name, out object result)
        {
            return this._dictionary.TryGetValue(name, out result);
        }

        public Type GetMemberType(string name)
        {
            if (this._dictionary.ContainsKey(name))
            {
                if (this._dictionary[name] == null) return typeof(object);

                return this._dictionary[name].GetType();
            }
            else
            {
                throw new ArgumentException(string.Format("'{0}' does not exist.", name));
            }
        }

        public bool SetMember(string name, object value)
        {
            if (!this._dictionary.ContainsKey(name))
            {
                this._dictionary.Add(name, value);
            }
            else
            {
                var valueType = this._dictionary[name].GetType();

                if (value != null)
                {
                    if (!value.GetType().IsAssignableFrom(valueType)) return false;   
                }

                this._dictionary[name] = value;

                this.OnPropertyChanged(name);
            }

            return true;
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return new PropertyDescriptorCollection(
                this.GetDynamicMemberNames()
                .Select(propertyName => new DynamicPropertyDescriptor(propertyName, this.GetType(), this.GetMemberType(propertyName)))
                .Cast<PropertyDescriptor>()
                .ToArray());
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(this, attributes, true);
        }

        public object GetPropertyOwner(PropertyDescriptor propertyDescriptor)
        {
            return this;
        }
    }
}