namespace DynamicObjectEx
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    public static class TypeExtensions
    {
        private static readonly Type[] SimpleTypes = new Type[]
                {
                    typeof(Enum),
                    typeof(String),
                    typeof(Decimal),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                    typeof(Guid)
                };

        internal static bool IsSimpleType(this Type type)
        {
            return
                type.IsPrimitive ||
                SimpleTypes.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                type.IsEnum ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]));
        }

        internal static bool IsStruct(this Type type)
        {
            return type.IsValueType && !type.IsPrimitive;
        }

        internal static bool IsCollection(this Type type)
        {
            if (type.IsSimpleType()) return false;
            if (type == typeof(IEnumerable)) return true;

            return type.GetInterfaces().Contains(typeof(IEnumerable));
        }

        internal static bool IsDictionary(this Type type)
        {
            if (typeof(IDictionary).IsAssignableFrom(type)) return true;
            if (type.ImplementsGenericDefinition(typeof(IDictionary<,>))) return true;
            if (type.ImplementsGenericDefinition(typeof(IReadOnlyDictionary<,>))) return true;

            return false;
        }

        internal static bool IsStack(this Type type)
        {
            if (type == typeof(Stack)) return true;
            if (type.IsGenericDefinition(typeof(Stack<>))) return true;
            if (type.IsGenericDefinition(typeof(ConcurrentStack<>))) return true;

            return false;
        }

        internal static Type GetCollectionItemType(this Type type)
        {
            var genericListType = default(Type);

            if (type.IsArray)
            {
                return type.GetElementType();
            }
            else if (type.ImplementsGenericDefinition(typeof(IEnumerable<>), out genericListType))
            {
                if (genericListType.IsGenericTypeDefinition)
                {
                    throw new Exception(string.Format("Type {0} is not a collection.", type.Name));
                }

                return genericListType.GetGenericArguments().Single();
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return null;
            }

            throw new Exception(string.Format("Type {0} is not a collection.", type.Name));
        }

        internal static bool ImplementsGenericDefinition(this Type type, Type genericInterfaceDefinition)
        {
            var implementingType = default(Type);

            return type.ImplementsGenericDefinition(genericInterfaceDefinition, out implementingType);
        }

        internal static bool ImplementsGenericDefinition(this Type type, Type genericInterfaceDefinition, out Type implementingType)
        {
            if (!genericInterfaceDefinition.IsInterface || !genericInterfaceDefinition.IsGenericTypeDefinition)
            {
                throw new ArgumentNullException("'{0}' is not a generic interface definition.");
            }

            if (type.IsInterface)
            {
                if (type.IsGenericType)
                {
                    Type interfaceDefinition = type.GetGenericTypeDefinition();

                    if (genericInterfaceDefinition == interfaceDefinition)
                    {
                        implementingType = type;
                        return true;
                    }
                }
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    Type interfaceDefinition = interfaceType.GetGenericTypeDefinition();

                    if (genericInterfaceDefinition == interfaceDefinition)
                    {
                        implementingType = interfaceType;
                        return true;
                    }
                }
            }

            implementingType = null;

            return false;
        }

        internal static bool IsKeyValuePair(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }

        internal static bool IsDictionaryEntry(this Type type)
        {
            return type == typeof(DictionaryEntry);
        }

        internal static void GetDictionaryKeyValueTypes(this Type type, out Type keyType, out Type valueType)
        {
            var dictionaryType = default(Type);

            if (type.ImplementsGenericDefinition(typeof(IDictionary<,>), out dictionaryType))
            {
                if (dictionaryType.IsGenericTypeDefinition)
                {
                    throw new Exception(string.Format("Type {0} is not a dictionary.", dictionaryType.Name));
                }

                Type[] dictionaryGenericArguments = dictionaryType.GetGenericArguments();

                keyType = dictionaryGenericArguments[0];
                valueType = dictionaryGenericArguments[1];
                return;
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                keyType = null;
                valueType = null;
                return;
            }

            throw new Exception(string.Format("Type {0} is not a dictionary.", type.Name));
        }

        internal static bool IsGenericDefinition(this Type type, Type genericInterfaceDefinition)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            return (type.GetGenericTypeDefinition() == genericInterfaceDefinition);
        }

        internal static object CreateInstance(this Type type, params object[] args)
        {
            return type.CreateInstance<object>(args);
        }

        internal static T CreateInstance<T>(this Type type, params object[] args)
        {
            if (args.Length == 0)
            {
                if (type.GetConstructor(Type.EmptyTypes) != null)
                {
                    T instance = (T)Activator.CreateInstance(type);

                    if (instance == null) return default(T);

                    return instance;
                }
                else
                {
                    return (T)FormatterServices.GetUninitializedObject(type);
                }
            }
            else if (args.Length > 0)
            {
                return (T)Activator.CreateInstance(type, args);
            }
            else
            {
                throw new Exception();
            }
        }

        internal static List<MemberInfo> GetFieldsAndProperties(this Type type)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            var memberInfos = new List<MemberInfo>();
            memberInfos.AddRange(type.GetFields(bindingFlags));
            memberInfos.AddRange(type.GetProperties(bindingFlags));

            if (!type.IsKeyValuePair())
            {
                var ignoreMemberInfos = memberInfos
                    .Where(item =>
                        item.HasObsoleteAttributeFromCache() ||
                        item.HasDynamicObjectExIgnoreAttributeFromCache() ||
                        item.GetValueType().IsGenericDefinition(typeof(EventHandler)) ||
                        item.GetValueType().IsGenericDefinition(typeof(EventHandler<>)) ||
                        !item.CanWrite());
                        
                ignoreMemberInfos = ignoreMemberInfos
                    .Where(item =>
                        !item.HasDynamicObjectExIncludeAttributeFromCache());

                memberInfos.RemoveAll(item => ignoreMemberInfos.Contains(item));

                memberInfos.ForEach(item =>
                {
                    DynamicObjectExHelper.CheckNotSupportedConversionType(item.GetValueType());
                });
            }

            return memberInfos;
        }

        internal static IWrappedCollection CreateCollectionWrapper(this Type type)
        {
            var genericArguments = type.GetCollectionItemType();
            var wrapperConstructorArgument = default(Type);
            var createdType = default(Type);

            if (type.IsArray)
            {
                wrapperConstructorArgument = typeof(ICollection<>).MakeGenericType(genericArguments);
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                type.ImplementsGenericDefinition(typeof(ICollection<>), out wrapperConstructorArgument);

                if (type == typeof(IList))
                {
                    createdType = typeof(List<object>);
                }
            }
            else if (type.ImplementsGenericDefinition(typeof(ICollection<>), out wrapperConstructorArgument))
            {
                if (type.IsGenericDefinition(typeof(ICollection<>)) || type.IsGenericDefinition(typeof(IList<>)))
                {
                    createdType = typeof(List<>).MakeGenericType(genericArguments);
                }
                else if (type.IsGenericDefinition(typeof(ISet<>)))
                {
                    createdType = typeof(HashSet<>).MakeGenericType(genericArguments);
                }
            }
            else if (type.ImplementsGenericDefinition(typeof(IReadOnlyCollection<>), out wrapperConstructorArgument))
            {
                if (type.IsGenericDefinition(typeof(IReadOnlyCollection<>)) || type.IsGenericDefinition(typeof(IReadOnlyList<>)))
                {
                    createdType = typeof(ReadOnlyCollection<>).MakeGenericType(genericArguments);
                }
            }
            else if (type.ImplementsGenericDefinition(typeof(IEnumerable<>), out wrapperConstructorArgument))
            {
                if (type.IsGenericDefinition(typeof(IEnumerable<>)))
                {
                    createdType = typeof(List<>).MakeGenericType(genericArguments);
                }
            }
            else if (typeof(ICollection).IsAssignableFrom(type))
            {
                if (type == typeof(ICollection))
                {
                    createdType = typeof(List<object>);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            var wrapper = typeof(CollectionWrapper<>).MakeGenericType(genericArguments ?? typeof(object));
            var wrapperConstructor = wrapper.GetConstructor(new Type[] { wrapperConstructorArgument ?? type });

            return wrapperConstructor.Invoke(
                new object[] {
                    (type.IsArray) ? typeof(List<>).MakeGenericType(genericArguments).CreateInstance() :
                    (createdType == null) ? type.CreateInstance() : createdType.CreateInstance() }) as IWrappedCollection;
        }

        internal static IWrappedDictionary CreateDictionaryWrapper(this Type type)
        {
            var keyType = default(Type);
            var valueType = default(Type);
            var createdType = default(Type);
            var dictionaryType = default(Type);

            if (type.ImplementsGenericDefinition(typeof(IDictionary<,>), out dictionaryType))
            {
                type.GetDictionaryKeyValueTypes(out keyType, out valueType);

                if (type.IsGenericDefinition(typeof(IDictionary<,>)))
                {
                    createdType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                }
            }
            else if (type.ImplementsGenericDefinition(typeof(IReadOnlyDictionary<,>), out dictionaryType))
            {
                type.GetDictionaryKeyValueTypes(out keyType, out valueType);

                if (type.IsGenericDefinition(typeof(IReadOnlyDictionary<,>)))
                {
                    createdType = typeof(ReadOnlyDictionary<,>).MakeGenericType(keyType, valueType);
                }
            }
            else
            {
                type.GetDictionaryKeyValueTypes(out keyType, out valueType);

                if (type == typeof(IDictionary))
                {
                    createdType = typeof(Dictionary<object, object>);
                }
            }

            var wrapper = typeof(DictionaryWrapper<,>).MakeGenericType(keyType ?? typeof(object), valueType ?? typeof(object));
            var dictionaryWrapperConstructor = wrapper.GetConstructor(new Type[] { dictionaryType ?? type });

            return dictionaryWrapperConstructor.Invoke(
                new object[] { (createdType == null) ? type.CreateInstance() : createdType.CreateInstance() }) as IWrappedDictionary;
        }
    }
}
