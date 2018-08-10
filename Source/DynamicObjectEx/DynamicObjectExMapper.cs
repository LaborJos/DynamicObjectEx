namespace DynamicObjectEx
{
    using System;
    using System.Collections;

    public static class DynamicObjectExMapper
    {
        public static void Map<T>(this IDynamicObjectEx source, T destination)
        {
            //GetValue(source, source.GetOriginalType(), destination);
            SetObject(source, ref destination);
        }

        public static T Map<T>(this IDynamicObjectEx source) where T : new()
        {
            var destination = typeof(T).CreateInstance<T>();

            source.Map(destination);

            return destination;
        }

        public static dynamic Map(this IDynamicObjectEx source)
        {
            var destination = source.GetOriginalType().CreateInstance();

            source.Map(destination);

            return destination;
        }

        private static dynamic GetValue(object source, Type sourceType, object destination = null)
        {
            var attribute = default(DynamicObjectExConverterAttribute);

            if (sourceType.TryGetAttributeFromCache(out attribute))
            {
                return attribute.Converter.ConvertBack(GetValue(source, attribute.ConvertType));
            }

            if (sourceType.IsSimpleType())
            {
                return source;
            }
            else if (sourceType.IsCollection())
            {
                return GetList(sourceType, source as IEnumerable);
            }
            else if (source is IDynamicObjectEx)
            {
                var dynamicObjectEx = source as IDynamicObjectEx;
                var originalType = dynamicObjectEx.GetOriginalType();

                if (originalType.IsKeyValuePair())
                {
                    return dynamicObjectEx.GetKeyValuePair(originalType);
                }
                else if (originalType.IsDictionaryEntry())
                {
                    return dynamicObjectEx.GetDictionaryEntry();
                }
                else
                {
                    if (sourceType.IsInterface || sourceType.IsAbstract || destination == null)
                    {
                        destination = dynamicObjectEx.GetOriginalType().CreateInstance<object>();
                    }

                    SetObject(dynamicObjectEx, ref destination);

                    return destination;
                }
            }
            else
            {
                throw new NotImplementedException(sourceType.Name);
            }
        }

        private static object GetKeyValuePair(this IDynamicObjectEx source, Type type)
        {
            var genericArgumentTypes = type.GetGenericArguments();

            if (genericArgumentTypes.Length != 2)
            {
                throw new Exception("Generic argument type length must be two.");
            }

            var keyType = genericArgumentTypes[0];
            var valueType = genericArgumentTypes[1];

            var key = GetValue(source.AsDynamic().Key, keyType) as object;
            var value = GetValue(source.AsDynamic().Value, valueType) as object;

            return type.CreateInstance(key, value);
        }

        private static object GetDictionaryEntry(this IDynamicObjectEx source)
        {
            var key = source.AsDynamic().Key;
            var value = source.AsDynamic().Value;

            return typeof(DictionaryEntry).CreateInstance(GetValue(key, key.GetType()) as object, GetValue(value, value.GetType()) as object);
        }

        private static void SetObject<T>(IDynamicObjectEx source, ref T destination)
        {
            var destinationType = destination.GetType();

            if (source.GetOriginalType() != destinationType)
            {
                throw new Exception("Original type of the converted object and the type of the object to be mapped are not the same.");
            }

            var memberInfos = destinationType.GetMemberInfosFromCache();

            for (var index = 0; index < memberInfos.Count; index++)
            {
                var memberInfo = memberInfos[index];
                var value = default(object);

                if (!source.GetMember(memberInfo.Name, out value)) continue;

                if (value == null)
                {
                    memberInfo.SetValue(destination, null);
                    continue;
                }

                var valueType = memberInfo.GetValueType();

                try
                {
                    var dynamicObjectEx = value as IDynamicObjectEx;

                    if (dynamicObjectEx != null && dynamicObjectEx.IsConverted)
                    {
                        var convertValue = GetValue(dynamicObjectEx, dynamicObjectEx.Converter.ConvertType);

                        memberInfo.SetValue(destination, dynamicObjectEx.Converter.ConvertBack(convertValue) as object);
                        continue;
                    }

                    var attribute = default(DynamicObjectExConverterAttribute);

                    if (memberInfo.TryGetAttributeFromCache(out attribute))
                    {
                        var convertValue = GetValue(value, attribute.ConvertType);
                        memberInfo.SetValue(destination, attribute.Converter.ConvertBack(convertValue) as object);
                        continue;
                    }

                    var safetyValue = DynamicObjectExHelper.GetSafetyValue(GetValue(value, valueType, memberInfo.GetValue(destination)), valueType) as object;

                    memberInfo.SetValue(destination, safetyValue);
                }
                catch (Exception /*ex*/)
                {
                    System.Diagnostics.Debug.Assert(false);
                    //throw ex;
                }
            }
        }

        private static dynamic GetList(Type type, IEnumerable enumerable)
        {
            if (enumerable == null) return null;

            if (type.IsDictionary())
            {
                var dictionary = type.CreateDictionaryWrapper() as IDictionary;

                foreach (var item in enumerable)
                {
                    var value = GetValue(item, item.GetType());

                    dictionary[value.Key] = value.Value;
                }

                return ((IWrappedDictionary)dictionary).UnderlyingDictionary;
            }
            else
            {
                var list = type.CreateCollectionWrapper() as IList;
                var itemType = type.GetCollectionItemType();

                foreach (var item in enumerable)
                {
                    var dynamicObjectEx = item as IDynamicObjectEx;
                    
                    list.Add(GetValue(item, item.GetType().IsCollection() ? itemType :
                        dynamicObjectEx != null ? (dynamicObjectEx.IsConverted ? dynamicObjectEx.Converter.OriginalType : item.GetType()) : item.GetType()));
                }

                if (type.IsStack())
                {
                    ((IWrappedCollection)list).Reverse();
                }

                if (type.IsArray)
                {
                    var array = Array.CreateInstance(type.GetElementType(), enumerable.Count());
                    list.CopyTo(array, 0);

                    return array;
                }
                else
                {
                    return ((IWrappedCollection)list).UnderlyingCollection;
                }
            }
        }
    }
}