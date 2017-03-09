using System.Collections.Immutable;

namespace Calliope
{
    internal static class CollectionsExtensions
    {
        public static bool DictionaryEquals<TKey, TVal>(
            this ImmutableDictionary<TKey, TVal> x,
            ImmutableDictionary<TKey, TVal> y)
        {
            if (x.Count != y.Count) return false;

            foreach (var entry in x)
            {
                TVal val;
                if (!y.TryGetValue(entry.Key, out val) || !Equals(val, entry.Value)) return false;
            }

            return true;
        }

        public static int DictionaryHashCode<TKey, TVal>(this ImmutableDictionary<TKey, TVal> x)
        {
            var hash = 0;
            foreach (var entry in x)
            {
                hash ^= (entry.Key.GetHashCode() * 397) ^ (entry.Value?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}