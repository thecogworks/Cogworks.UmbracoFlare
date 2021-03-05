using System.Collections.Generic;
using System.Linq;

namespace Cogworks.UmbracoFlare.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool HasAny<T>(this IEnumerable<T> items)
            => items != null && items.Any();

        public static void AddRangeUnique<T>(this IList<T> self, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (!self.Contains(item))
                {
                    self.Add(item);
                }
            }
        }

        public static void AddUnique<T>(this IList<T> self, T item)
        {
            if (!self.Contains(item))
            {
                self.Add(item);
            }
        }
    }
}