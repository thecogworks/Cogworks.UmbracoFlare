using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Code found on Stackoverflow by casperOne Ref: https://stackoverflow.com/a/419058/150415
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            // Validate parameters.
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize),
                "The chunkSize parameter must be a positive value.");

            // Call the internal implementation.
            return source.ChunkInternal(chunkSize);
        }

        // Code found on Stackoverflow by casperOne Ref: https://stackoverflow.com/a/419058/150415
        private static IEnumerable<IEnumerable<T>> ChunkInternal<T>(this IEnumerable<T> source, int chunkSize)
        {
            // Validate parameters.
            Debug.Assert(source != null);
            Debug.Assert(chunkSize > 0);

            // Get the enumerator.  Dispose of when done.
            using (IEnumerator<T> enumerator = source.GetEnumerator())
                do
                {
                    // Move to the next element.  If there's nothing left
                    // then get out.
                    if (!enumerator.MoveNext()) yield break;

                    // Return the chunked sequence.
                    yield return ChunkSequence(enumerator, chunkSize);
                } while (true);
        }

        // Code found on Stackoverflow by casperOne Ref: https://stackoverflow.com/a/419058/150415
        private static IEnumerable<T> ChunkSequence<T>(IEnumerator<T> enumerator, int chunkSize)
        {
            // Validate parameters.
            Debug.Assert(enumerator != null);
            Debug.Assert(chunkSize > 0);

            // The count.
            int count = 0;

            // There is at least one item.  Yield and then continue.
            do
            {
                // Yield the item.
                yield return enumerator.Current;
            } while (++count < chunkSize && enumerator.MoveNext());
        }
    }
}