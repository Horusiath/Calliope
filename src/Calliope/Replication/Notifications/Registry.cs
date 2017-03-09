using System.Collections.Immutable;

namespace Calliope.Replication.Notifications
{
    /// <summary>
    /// An immutable registry serving as both multi-value dictionary and reverted index.
    /// </summary>
    /// <typeparam name="T1">Type used for grouping values of type <typeparamref name="T2"/></typeparam>
    /// <typeparam name="T2">Type of which values are grouped. Also used for reverted index.</typeparam>
    internal sealed class Registry<T1, T2>
    {
        private readonly ImmutableDictionary<T1, ImmutableHashSet<T2>> registry;
        private readonly ImmutableDictionary<T2, T1> index;

        public Registry(
            ImmutableDictionary<T1, ImmutableHashSet<T2>> registry = null,
            ImmutableDictionary<T2, T1> index = null)
        {
            this.registry = registry ?? ImmutableDictionary<T1, ImmutableHashSet<T2>>.Empty;
            this.index = index ?? ImmutableDictionary<T2, T1>.Empty;
        }

        /// <summary>
        /// Returns an <see cref="ImmutableHashSet{T}"/> of <typeparamref name="T2"/> values stored for 
        /// a given <paramref name="key"/> (or empty set, if nothing was found).
        /// </summary>
        public ImmutableHashSet<T2> this[T1 key] => registry.GetValueOrDefault(key, ImmutableHashSet<T2>.Empty);

        /// <summary>
        /// Returns a <typeparamref name="T1"/> value for a given <paramref name="revertedKey"/> from reverted index.
        /// </summary>
        public T1 this[T2 revertedKey] => index.GetValueOrDefault(revertedKey, default(T1));

        /// <summary>
        /// Adds a <paramref name="reverted"/> value into a bucket for stored <paramref name="key"/> and applies 
        /// it into reverted index (where <paramref name="reverted"/> is treated as a key and <paramref name="key"/> 
        /// is a value). Returns a new registry with updated values.
        /// </summary>
        public Registry<T1, T2> Add(T1 key, T2 reverted)
        {
            ImmutableHashSet<T2> bucket;
            bucket = !registry.TryGetValue(key, out bucket)
                ? ImmutableHashSet.Create(reverted)
                : bucket.Add(reverted);

            return new Registry<T1, T2>(
                registry: this.registry.SetItem(key, bucket),
                index: this.index.SetItem(reverted, key));
        }

        /// <summary>
        /// Removes a <paramref name="reverted"/> value from a bucket stored under provided 
        /// <paramref name="key"/>. Also removes this pair from reverted index. 
        /// Returns a new instance of a registry.
        /// </summary>
        public Registry<T1, T2> Remove(T1 key, T2 reverted)
        {
            ImmutableHashSet<T2> bucket;
            bucket = !registry.TryGetValue(key, out bucket)
                ? ImmutableHashSet<T2>.Empty
                : bucket.Remove(reverted);

            if (bucket.IsEmpty)
                return new Registry<T1, T2>(
                    registry: this.registry.Remove(key),
                    index: this.index.Remove(reverted));
            else
                return new Registry<T1, T2>(
                    registry: this.registry.SetItem(key, bucket),
                    index: this.index.Remove(reverted));
        }
    }
}