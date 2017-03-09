using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Calliope.Versioning
{
    /// <summary>
    /// Vector time represented as map of replicaId -> logical time.
    /// </summary>
    public struct VectorTime : IComparable<VectorTime>, IEquatable<VectorTime>, IMergeable<VectorTime>, IPartiallyComparable<VectorTime>, IComparable
    {
        #region comparer
        private class VectorTimeComparer : IPartialComparer<VectorTime>, IComparer<VectorTime>
        {
            public static readonly VectorTimeComparer Instance = new VectorTimeComparer();

            private VectorTimeComparer() { }
            public int? PartiallyCompare(VectorTime x, VectorTime y)
            {
                var xval = x.Value ?? ImmutableDictionary<string, long>.Empty;
                var yval = y.Value ?? ImmutableDictionary<string, long>.Empty;
                var keys = xval.Keys.Union(yval.Keys).Distinct();
                var current = 0;
                foreach (var key in keys)
                {
                    var x1 = xval.GetValueOrDefault(key, 0L);
                    var y2 = yval.GetValueOrDefault(key, 0L);
                    var s = Math.Sign(x1 - y2);

                    if (current == 0L)
                    {
                        current = s;
                    }
                    else if (current == -1)
                    {
                        if (s == 1) return null;
                    }
                    else // current == +1
                    {
                        if (s == -1) return null;
                    }
                }

                return current;
            }

            public int Compare(VectorTime x, VectorTime y)
            {
                var compareResult = PartiallyCompare(x, y);
                if (compareResult.HasValue)
                    return compareResult.Value;
                else
                {
                    throw new InvalidOperationException($"State of {x} and {y} doesn't allow to compare those values");
                }
            }
        }
        #endregion

        /// <summary>
        /// <see cref="IComparer{T}"/> instance for the <see cref="VectorTime"/>.
        /// </summary>
        public static readonly IComparer<VectorTime> Comparer = VectorTimeComparer.Instance;

        /// <summary>
        /// <see cref="IPartialComparer{T}"/> instance for the <see cref="VectorTime"/>
        /// </summary>
        public static readonly IPartialComparer<VectorTime> EqualityComparer = VectorTimeComparer.Instance;

        /// <summary>
        /// A zero value for <see cref="VectorTime"/>.
        /// </summary>
        public static readonly VectorTime Zero = new VectorTime(ImmutableDictionary<string, long>.Empty);

        /// <summary>
        /// Creates a new instance of a <see cref="VectorTime"/> with <paramref name="value"/> set for target replica.
        /// </summary>
        public static VectorTime Create(string replicaId, long value = 1L) =>
            new VectorTime(new KeyValuePair<string, long>(replicaId, value));

        /// <summary>
        /// A versioned vector time value - it consists of map of replicaId->logical time for each replica.
        /// </summary>
        public readonly IImmutableDictionary<string, long> Value;

        public VectorTime(IImmutableDictionary<string, long> value) : this()
        {
            Value = value ?? ImmutableDictionary<string, long>.Empty;
        }

        public VectorTime(params KeyValuePair<string, long>[] pairs) : this(ImmutableDictionary.CreateRange(pairs)) { }

        /// <summary>
        /// Sets a <paramref name="localTime"/> value for target <paramref name="replicaId"/>, 
        /// returning new <see cref="VectorTime"/> in the result.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorTime SetTime(string replicaId, long localTime) =>
            new VectorTime((Value ?? ImmutableDictionary<string, long>.Empty).SetItem(replicaId, localTime));

        /// <summary>
        /// Returns a local time value for target <paramref name="replicaId"/> 
        /// stored in current <see cref="VectorTime"/>.
        /// </summary>
        public long this[string replicaId] => Value?.GetValueOrDefault(replicaId, 0L) ?? 0L;

        /// <summary>
        /// Returns a new instance of the <see cref="VectorTime"/> containing 
        /// only information about target <paramref name="replicaId"/>.
        /// </summary>
        [Pure]
        public VectorTime CopyOne(string replicaId)
        {
            long time;
            return Value != null && Value.TryGetValue(replicaId, out time)
                ? new VectorTime(ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, long>(replicaId, time) }))
                : new VectorTime(ImmutableDictionary<string, long>.Empty);
        }

        /// <summary>
        /// Increments a logical time value for a target <paramref name="replicaId"/>,
        /// returning new instance of <see cref="VectorTime"/> in the result.
        /// </summary>
        [Pure]
        public VectorTime Increment(string replicaId)
        {
            long time;
            return Value != null && Value.TryGetValue(replicaId, out time)
                ? new VectorTime(Value.SetItem(replicaId, time + 1))
                : new VectorTime((Value ?? ImmutableDictionary<string, long>.Empty).SetItem(replicaId, 1L));
        }

        /// <inheritdoc cref="IMergeable{T}"/>
        [Pure]
        public VectorTime Merge(VectorTime other)
        {
            var x = Value ?? ImmutableDictionary<string, long>.Empty;
            var y = other.Value ?? ImmutableDictionary<string, long>.Empty;
            var dict = x.Union(y)
                .Aggregate(ImmutableDictionary<string, long>.Empty, (map, pair) =>
                    map.SetItem(pair.Key, Math.Max(map.GetValueOrDefault(pair.Key, long.MinValue), pair.Value)));

            return new VectorTime(dict);
        }

        /// <summary>
        /// Checks if current <see cref="VectorTime"/> in concurrent to provided one.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsConcurrent(VectorTime other) =>
            !VectorTimeComparer.Instance.PartiallyCompare(this, other).HasValue;

        public bool Equals(VectorTime other) => VectorTimeComparer.Instance.PartiallyCompare(this, other) == 0;

        public override bool Equals(object obj)
        {
            if (obj is VectorTime) return Equals((VectorTime)obj);
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 0;
                foreach (var entry in this.Value)
                {
                    hash ^= (entry.Key.GetHashCode() * 397) ^ entry.Value.GetHashCode();
                    hash = (hash << 7) | (hash >> (32 - 7));
                }
                return hash;
            }
        }

        public override string ToString() => $"VectorTime({string.Join("; ", Value.Select(p => $"{p.Key}:{p.Value}"))})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(VectorTime other) => VectorTimeComparer.Instance.Compare(this, other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object obj)
        {
            if (obj is VectorTime) return CompareTo((VectorTime)obj);
            else
            {
                throw new ArgumentException($"Cannot compare values of type {GetType()} and {obj?.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) == -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) < 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) > -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? PartiallyCompareTo(VectorTime other) => VectorTimeComparer.Instance.PartiallyCompare(this, other);
    }
}