using System;
using System.Linq;
using System.Linq.Expressions;

namespace DynamicObjectEx
{
    public interface IDynamicObjectExConverter
    {
        Type ConvertType { get; }
        Type OriginalType { get; }
        dynamic Convert(dynamic value);
        dynamic ConvertBack(dynamic value);
    }
    
    public abstract class DynamicObjectExConverter<TOriginalType, TConvertType> : IDynamicObjectExConverter
    {
        public Type ConvertType { get { return typeof(TConvertType); } }
        public Type OriginalType { get { return typeof(TOriginalType); } }

        public abstract TConvertType Convert(TOriginalType value);

        dynamic IDynamicObjectExConverter.Convert(dynamic value)
        {
            return this.Convert(value);
        }

        public abstract TOriginalType ConvertBack(TConvertType value);

        dynamic IDynamicObjectExConverter.ConvertBack(dynamic value)
        {
            return this.ConvertBack(value);
        }
    }

    public abstract class DynamicObjectExWrapper<TOriginalType> : IDynamicObjectExConverter where TOriginalType : class
    {
        private Func<object, object> _convert;
        private Func<object, object> _convertBack;

        public Type ConvertType { get { return this.GetType(); } }
        public Type OriginalType { get { return typeof(TOriginalType); } }

        public DynamicObjectExWrapper()
        {
            this._convert = MakeConverter(this.OriginalType, this.ConvertType);
            this._convertBack = MakeConverter(this.ConvertType, this.OriginalType);
        }

        dynamic IDynamicObjectExConverter.Convert(dynamic value)
        {
            return this._convert(value);
        }

        dynamic IDynamicObjectExConverter.ConvertBack(dynamic value)
        {
            return this._convertBack(value);
        }

        private static Func<object, object> MakeConverter(Type typeFrom, Type typeTo)
        {
            var parameterExpression = Expression.Parameter(typeof(object));
            var expressionFrom = Expression.Convert(parameterExpression, typeFrom);
            var expressionTo = Expression.Convert(expressionFrom, typeTo);
            var result = Expression.Convert(expressionTo, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(result, new[] { parameterExpression });
            return lambda.Compile();
        }
    }
}
