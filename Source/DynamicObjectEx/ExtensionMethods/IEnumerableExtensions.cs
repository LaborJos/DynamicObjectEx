namespace DynamicObjectEx
{
    using System;
    using System.Collections;

    internal static class IEnumerableExtensions
    {
        internal static int Count(this IEnumerable source)
        {
            if (source is ICollection)
            {
                return (source as ICollection).Count;
            }

            int count = 0;
            var enumerator = source.GetEnumerator();

            DynamicUsing(enumerator, () =>
            {
                while (enumerator.MoveNext())
                {
                    count++;
                }
            });

            return count;
        }

        internal static void DynamicUsing(object resource, Action action)
        {
            try
            {
                action();
            }
            finally
            {
                if (resource is IDisposable)
                {
                    (resource as IDisposable).Dispose();
                }
            }
        }
    }
}