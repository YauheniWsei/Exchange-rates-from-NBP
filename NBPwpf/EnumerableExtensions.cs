using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBPwpf
{
    public static class EnumerableExtensions
    {
        public static T MaxBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector) where TKey : IComparable<TKey>
        {
            return source.OrderByDescending(selector).FirstOrDefault();
        }
    }
}
