using System;
using System.Collections.Generic;
using System.Reflection;

namespace DynamicObjectEx
{
    public static class CacheRepositories
    {
        internal readonly static Dictionary<Type, Dictionary<int, DynamicObjectExConverterAttribute>> DynamicObjectExConverterAttributeCache =
            new Dictionary<Type, Dictionary<int, DynamicObjectExConverterAttribute>>();
        internal readonly static Dictionary<Type, Dictionary<int, DynamicObjectExIncludeAttribute>> DynamicObjectExIncludeAttributeCache =
            new Dictionary<Type, Dictionary<int, DynamicObjectExIncludeAttribute>>();
        internal readonly static Dictionary<Type, Dictionary<int, DynamicObjectExIgnoreAttribute>> DynamicObjectExIgnoreAttributeCache =
            new Dictionary<Type, Dictionary<int, DynamicObjectExIgnoreAttribute>>();
        internal readonly static Dictionary<Type, Dictionary<int, XmlElementNameToTypeNameAttribute>> XmlElementNameToTypeNameAttributeCache =
            new Dictionary<Type, Dictionary<int, XmlElementNameToTypeNameAttribute>>();
        internal readonly static Dictionary<Type, Dictionary<int, ObsoleteAttribute>> ObsoleteAttributeCache =
            new Dictionary<Type, Dictionary<int, ObsoleteAttribute>>();
        internal readonly static Dictionary<Type, List<MemberInfo>> MemberInfosCache =
            new Dictionary<Type, List<MemberInfo>>();

        internal static bool TryGetAttributeFromCache(this MemberInfo memberInfo, out DynamicObjectExConverterAttribute attribute)
        {
            return memberInfo.TryGetAttributeFromCache(DynamicObjectExConverterAttributeCache, out attribute);
        }

        private static bool TryGetAttributeFromCache<T>(this MemberInfo memberInfo, Dictionary<Type, Dictionary<int, T>> cache, out T attribute) where T : Attribute
        {
            var type = memberInfo.DeclaringType ?? memberInfo as Type;

            if (!cache.ContainsKey(type))
            {
                cache[type] = new Dictionary<int, T>();
            }

            var key = memberInfo.MetadataToken;

            if (!cache[type].ContainsKey(key))
            {
                cache[type][key] = memberInfo.GetAttribute<T>();
            }

            attribute = cache[type][key];

            return (attribute != null);
        }

        internal static List<MemberInfo> GetMemberInfosFromCache(this Type type)
        {
            if (!MemberInfosCache.ContainsKey(type))
            {
                MemberInfosCache[type] = type.GetFieldsAndProperties();
            }

            return MemberInfosCache[type];
        }

        internal static bool HasDynamicObjectExIgnoreAttributeFromCache(this MemberInfo memberInfo)
        {
            return memberInfo.HasAttributeFromCache(DynamicObjectExIgnoreAttributeCache);
        }

        internal static bool HasDynamicObjectExIncludeAttributeFromCache(this MemberInfo memberInfo)
        {
            return memberInfo.HasAttributeFromCache(DynamicObjectExIncludeAttributeCache);
        }

        internal static bool HasObsoleteAttributeFromCache(this MemberInfo memberInfo)
        {
            return memberInfo.HasAttributeFromCache(ObsoleteAttributeCache);
        }

        internal static bool HasXmlElementNameToTypeNameAttributeFromCache(this MemberInfo memberInfo)
        {
            return memberInfo.HasAttributeFromCache(XmlElementNameToTypeNameAttributeCache);
        }

        private static bool HasAttributeFromCache<T>(this MemberInfo memberInfo, Dictionary<Type, Dictionary<int, T>> cache) where T : Attribute
        {
            T attribute;
            return (memberInfo.TryGetAttributeFromCache<T>(cache, out attribute));
        }
    }
}
