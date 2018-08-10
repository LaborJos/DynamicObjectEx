using System;
using System.Collections;

namespace DynamicObjectEx
{
    public static class DynamicObjectExFactory
    {
        public static IDynamicObjectEx ToDynamicObjectEx<T>(this T target)
        {
            return GetObject(target);
        }

        private static object GetValue<T>(T target, IDynamicObjectExConverter converter = null)
        {
            dynamic value = target;
            var valueType = typeof(T);

            if (converter == null && valueType.TryGetAttributeFromCache(out DynamicObjectExConverterAttribute attribute))
            {
                value = attribute.Converter.Convert(value);
                valueType = attribute.ConvertType;
                converter = attribute.Converter;
            }

            if (valueType.IsSimpleType())
            {
                return value;
            }
            else if (valueType.IsCollection())
            {
                return GetEnumerable(value as IEnumerable);
            }
            else
            {
                return GetObject(value, converter);
            }
        }

        private static IDynamicObjectEx GetObject<T>(T target, IDynamicObjectExConverter converter = null)
        {
            var targetType = target.GetType();
            var dynamicObjectEx = typeof(DynamicObjectEx<>).MakeGenericType(targetType).CreateInstance(converter) as IDynamicObjectEx;
            var memberInfos = targetType.GetMemberInfosFromCache();

            for (var index = 0; index < memberInfos.Count; index++)
            {
                try
                {
                    var memberInfo = memberInfos[index];
                    var value = memberInfo.GetValue(target);

                    if (value == null)
                    {
                        dynamicObjectEx.SetMember(memberInfo.Name, null);
                        continue;
                    }

                    if (memberInfo.TryGetAttributeFromCache(out DynamicObjectExConverterAttribute converterAttribute))
                    {
                        dynamicObjectEx.SetMember(memberInfo.Name, GetValue(converterAttribute.Converter.Convert(value), converterAttribute.Converter));
                    }
                    else
                    {
                        dynamicObjectEx.SetMember(memberInfo.Name, GetValue(value));
                    }
                }
                catch (Exception)
                {
                }
            }

            return dynamicObjectEx;
        }
        
        private static IEnumerable GetEnumerable(IEnumerable enumerable)
        {
            var itemType = enumerable.GetType().GetCollectionItemType();
            var attribute = default(DynamicObjectExConverterAttribute);

            var list = (itemType == null || itemType.IsSimpleType() || itemType.IsCollection()) ?
                typeof(DynamicObjectExCollection<>).MakeGenericType(typeof(object)).CreateInstance() as IList :
                itemType.TryGetAttributeFromCache(out attribute) ?
                typeof(DynamicObjectExCollection<>).MakeGenericType(typeof(DynamicObjectEx<>).MakeGenericType(attribute.ConvertType)).CreateInstance() as IList :
                typeof(DynamicObjectExCollection<>).MakeGenericType(typeof(DynamicObjectEx<>).MakeGenericType(itemType)).CreateInstance() as IList;

            foreach (var item in enumerable)
            {
                if (item == null)
                {
                    list.Add(null);
                }
                else
                {
                    list.Add(GetValue(item));
                }
            }

            return list;
        }
    }
}
