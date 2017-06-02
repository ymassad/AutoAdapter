using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoAdapter.Fody
{
    public static class EnumerableExtensionMethods
    {
        public static Maybe<T> FirstOrNoValue<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is IList<T> list)
            {
                if (list.Count > 0) return list[0];
            }
            else
            {
                using (IEnumerator<T> e = enumerable.GetEnumerator())
                {
                    if (e.MoveNext()) return e.Current;
                }
            }

            return Maybe<T>.NoValue();
        }

        public static Maybe<T> FirstOrNoValue<T>(this IEnumerable<T> enumerable, Func<T,bool> predicate)
        {
            return enumerable.Where(predicate).FirstOrNoValue();
        }
    }
}