#region copyright
//-----------------------------------------------------------------------
// <copyright file="VClock.cs" creator="Bartosz Sypytkowski">
//     Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
//-----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Calliope
{
    using ReplicaId = String;

    /// <summary>
    /// Vector clock represented as map of replicaId -> logical time.
    /// </summary>
    public struct VClock : IEquatable<VClock>, IPartiallyComparable<VClock>
    {
        #region comparer
        private sealed class VectorTimeComparer : IPartialComparer<VClock>
        {
            public static readonly VectorTimeComparer Instance = new VectorTimeComparer();

            private VectorTimeComparer() { }
            public int? PartiallyCompare(VClock x, VClock y)
            {
                var xval = x.Value ?? ImmutableDictionary<ReplicaId, long>.Empty;
                var yval = y.Value ?? ImmutableDictionary<ReplicaId, long>.Empty;
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
        }
        #endregion
        
        /// <summary>
        /// <see cref="IPartialComparer{T}"/> instance for the <see cref="VClock"/>
        /// </summary>
        public static readonly IPartialComparer<VClock> EqualityComparer = VectorTimeComparer.Instance;

        /// <summary>
        /// A zero value for <see cref="VClock"/>.
        /// </summary>
        public static readonly VClock Zero = new VClock(ImmutableDictionary<ReplicaId, long>.Empty);

        /// <summary>
        /// Creates a new instance of a <see cref="VClock"/> with <paramref name="value"/> set for target replica.
        /// </summary>
        public static VClock Create(ReplicaId replicaId, long value = 1L) =>
            new VClock(new KeyValuePair<ReplicaId, long>(replicaId, value));

        /// <summary>
        /// A versioned vector time value - it consists of map of replicaId->logical time for each replica.
        /// </summary>
        public readonly ImmutableDictionary<ReplicaId, long> Value;

        public VClock(ImmutableDictionary<ReplicaId, long> value) : this()
        {
            Value = value ?? ImmutableDictionary<ReplicaId, long>.Empty;
        }

        public VClock(params KeyValuePair<ReplicaId, long>[] pairs) : this(ImmutableDictionary.CreateRange(pairs)) { }

        /// <summary>
        /// Sets a <paramref name="localTime"/> value for target <paramref name="replicaId"/>, 
        /// returning new <see cref="VClock"/> in the result.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VClock SetTime(ReplicaId replicaId, long localTime) =>
            new VClock((Value ?? ImmutableDictionary<ReplicaId, long>.Empty).SetItem(replicaId, localTime));

        /// <summary>
        /// Returns a local time value for target <paramref name="replicaId"/> 
        /// stored in current <see cref="VClock"/>.
        /// </summary>
        public long this[ReplicaId replicaId] => Value?.GetValueOrDefault(replicaId, 0L) ?? 0L;

        /// <summary>
        /// Returns a new instance of the <see cref="VClock"/> containing 
        /// only information about target <paramref name="replicaId"/>.
        /// </summary>
        public VClock CopyOne(string replicaId)
        {
            long time;
            return Value != null && Value.TryGetValue(replicaId, out time)
                ? new VClock(ImmutableDictionary.CreateRange(new[] { new KeyValuePair<ReplicaId, long>(replicaId, time) }))
                : new VClock(ImmutableDictionary<ReplicaId, long>.Empty);
        }

        /// <summary>
        /// Increments a logical time value for a target <paramref name="replicaId"/>,
        /// returning new instance of <see cref="VClock"/> in the result.
        /// </summary>
        public VClock Increment(ReplicaId replicaId)
        {
            long time;
            return Value != null && Value.TryGetValue(replicaId, out time)
                ? new VClock(Value.SetItem(replicaId, time + 1))
                : new VClock((Value ?? ImmutableDictionary<ReplicaId, long>.Empty).SetItem(replicaId, 1L));
        }

        /// <summary>
        /// Merges current instance with another one, automatically and deterministically resolving all conflicts.
        /// Merge operation should be associative, commutative and idempotent.
        /// </summary>
        /// <param name="other">Other instance of the same type.</param>
        /// <returns></returns>
        public VClock Merge(VClock other)
        {
            var x = Value ?? ImmutableDictionary<ReplicaId, long>.Empty;
            var y = other.Value ?? ImmutableDictionary<ReplicaId, long>.Empty;
            var dict = x.Union(y)
                .Aggregate(ImmutableDictionary<ReplicaId, long>.Empty, (map, pair) =>
                    map.SetItem(pair.Key, Math.Max(map.GetValueOrDefault(pair.Key, long.MinValue), pair.Value)));

            return new VClock(dict);
        }

        /// <summary>
        /// Subtracts dots from current vector time by removing all entries with a corresponding keys found
        /// in <paramref name="other"/> vector time that have clock values &gt;= clock values of the current clock.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public VClock Subtract(VClock other)
        {
            if (other.Value == null || other.Value.IsEmpty) return this;

            var x = (Value ?? ImmutableDictionary<ReplicaId, long>.Empty).ToBuilder();
            foreach (var entry in other.Value)
            {
                if (x.TryGetValue(entry.Key, out var xval) && xval <= entry.Value)
                    x.Remove(entry.Key);
            }

            return new VClock(x.ToImmutable());
        }

        /// <summary>
        /// Checks if current <see cref="VClock"/> in concurrent to provided one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsConcurrent(VClock other) =>
            !VectorTimeComparer.Instance.PartiallyCompare(this, other).HasValue;

        public bool Equals(VClock other) => VectorTimeComparer.Instance.PartiallyCompare(this, other) == 0;

        public override bool Equals(object obj) => (obj is VClock vtime) && Equals(vtime);

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

        public override string ToString() => $"{{{string.Join("; ", Value.Select(p => $"{p.Key}:{p.Value}"))}}}";
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VClock x, VClock y) => x.PartiallyCompareTo(y) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VClock x, VClock y) => x.PartiallyCompareTo(y) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(VClock x, VClock y) => x.PartiallyCompareTo(y) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(VClock x, VClock y) => x.PartiallyCompareTo(y) == -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(VClock x, VClock y) => x.PartiallyCompareTo(y) < 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(VClock x, VClock y) => x.PartiallyCompareTo(y) > -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? PartiallyCompareTo(VClock other) => VectorTimeComparer.Instance.PartiallyCompare(this, other);
    }
}