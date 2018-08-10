namespace DynamicObjectEx
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;

    public class DynamicObjectExHelper
    {
        private readonly static ReadOnlyCollection<Type> _notSupportedConversionTypeList = new ReadOnlyCollection<Type>(
            new List<Type>()
            {
                typeof(System.Data.DataTable),
                //typeof(EventHandler),
            });

        private readonly static ReadOnlyCollection<Type> _notSupportedConversionGenericTypeList = new ReadOnlyCollection<Type>(
            new List<Type>()
            {
                //typeof(EventHandler<>),
            });

        private readonly static ReadOnlyCollection<Type> _notSupportedSerializationType = new ReadOnlyCollection<Type>(
            new List<Type>()
            {
                typeof(SortedList),
                typeof(Hashtable),
                typeof(ListDictionary),
            });

        internal static bool IsSupportedConversionType(Type type)
        {
            if (type.IsGenericType)
            {
                if (_notSupportedConversionGenericTypeList.Any(t => type.IsGenericDefinition(t))) return false;
            }
            else
            {
                if (_notSupportedConversionTypeList.Contains(type)) return false;
            }

            return true;
        }

        internal static void CheckNotSupportedConversionType(Type type)
        {
            if (!IsSupportedConversionType(type)) throw new NotSupportedException(type.Name);
        }

        internal static dynamic GetSafetyValue(object value, Type type)
        {
            if (value.GetType() == type) return value;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (!underlyingType.GetInterfaces().Contains(typeof(IConvertible))) return value;

            try
            {
                if (underlyingType.IsEnum)
                {
                    return Enum.Parse(underlyingType, value.ToString());
                }
                else
                {
                    return (value == null || string.IsNullOrEmpty(value.ToString())) ? null : Convert.ChangeType(value, underlyingType);
                }
            }
            catch (System.Exception e)
            {
                // Variable 일때는, String 값만 존재하는 경우가 있으므로 예외코드 필요함.
                System.Diagnostics.Debug.Assert(false);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
                return null;
            }
        }
    }
}