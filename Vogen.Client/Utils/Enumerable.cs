using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.Utils
{
    public static class Enumerable
    {
        public static IEnumerable<(T, T)> Pairwise<T>(this IEnumerable<T> items)
        {
            var iter = items.GetEnumerator();
            if (iter.MoveNext())
            {
                var prevItem = iter.Current;
                while (iter.MoveNext())
                {
                    yield return (prevItem, iter.Current);
                    prevItem = iter.Current;
                }
            }
        }
    }
}
