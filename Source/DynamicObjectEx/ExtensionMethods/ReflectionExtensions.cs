namespace DynamicObjectEx
{
    using System;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionExtensions
    {
        internal static T GetAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return Attribute.GetCustomAttributes(memberInfo, true).SingleOrDefault(attribute => attribute.GetType() == typeof(T)) as T;
        }

        /// <summary>
        /// 지정된 개체의 속성 값을 반환합니다.
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <param name="target">속성 값이 반환될 개체입니다.</param>
        /// <returns>지정된 개체의 속성 값입니다.</returns>
        internal static object GetValue<T>(this MemberInfo memberInfo, T target)
        {
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                return (memberInfo as FieldInfo).GetValue(target);
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                var propertyInfo = memberInfo as PropertyInfo;

                try
                {
                    if (propertyInfo.CanRead)
                    {
                        return propertyInfo.GetValue(target);
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                throw new ArgumentException("Property must be of type FieldInfo or PropertyInfo", "memberInfo");
            }
        }

        internal static Type GetValueType(this MemberInfo memberInfo)
        {
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                return (memberInfo as FieldInfo).FieldType;
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                return (memberInfo as PropertyInfo).PropertyType;
            }
            else
            {
                throw new ArgumentException("Property must be of type FieldInfo or PropertyInfo", "memberInfo");
            }
        }

        internal static void SetValue(this MemberInfo memberInfo, object target, dynamic value)
        {
            if (!memberInfo.CanWrite()) return;

            if (memberInfo.MemberType == MemberTypes.Field)
            {
                (memberInfo as FieldInfo).SetValue(target, value);
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                var propertyInfo = memberInfo as PropertyInfo;

                if (propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(target, value);
                }
            }
            else
            {
                throw new ArgumentException("Property must be of type FieldInfo or PropertyInfo", "memberInfo");
            }
        }

        internal static bool CanWrite(this MemberInfo memberInfo)
        {
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                return !(memberInfo as FieldInfo).IsInitOnly;
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                return (memberInfo as PropertyInfo).GetSetMethod(false) != null;
            }
            else
            {
                throw new ArgumentException("Property must be of type FieldInfo or PropertyInfo", "memberInfo");
            }
        }
    }
}