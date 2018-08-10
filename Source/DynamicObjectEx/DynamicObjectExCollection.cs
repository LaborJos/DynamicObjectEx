namespace DynamicObjectEx
{
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;

    public sealed class DynamicObjectExCollection<T> : ObservableCollection<T>, IList, ITypedList
    {
        public string GetListName(PropertyDescriptor[] listAccessors)
        {
            return null;
        }

        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            if (this.Any()) return TypeDescriptor.GetProperties(this[0]);
            else return null;
        }
    }
}
