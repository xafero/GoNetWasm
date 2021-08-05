using System;
using System.Collections.Generic;
using System.Linq;

namespace GoNetWasm.Internal
{
    internal static class Buffers
    {
        internal static void Refill<T>(this IDictionary<int, T> values, IEnumerable<T> rawObjects)
        {
            var objects = rawObjects.ToArray();
            values.Clear();
            for (var i = 0; i < objects.Length; i++)
                values[i] = objects[i];
        }

        internal static void Refill<T>(this IDictionary<T, int> values, IEnumerable<(T, int)> objects)
        {
            values.Clear();
            foreach (var entry in objects)
                values[entry.Item1] = entry.Item2;
        }

        internal static void Refill(this Span<byte> dest, ICollection<byte> source)
        {
            using var bytes = source.GetEnumerator();
            for (var i = 0; i < source.Count; i++)
            {
                bytes.MoveNext();
                dest[i] = bytes.Current;
            }
        }

        internal static void Refill(this ICollection<byte> dest, Span<byte> source)
        {
            dest.Clear();
            foreach (var t in source)
                dest.Add(t);
        }
    }
}