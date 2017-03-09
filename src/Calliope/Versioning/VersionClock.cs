using System;
using System.Runtime.CompilerServices;

namespace Calliope.Versioning
{
    /// <summary>
    /// VersionClock which combines abilities of partial time comparison of 
    /// <see cref="VectorTime"/> with support of monotonically increasing 
    /// <see cref="SequenceNr"/> to resolve cases when concurrent updates 
    /// were detected.
    /// </summary>
    public struct VersionClock : IEquatable<VersionClock>, IComparable<VersionClock>, IComparable, IMergeable<VersionClock>
    {
        /// <summary>
        /// Monotonically increasing number of local replica instance.
        /// </summary>
        public long SequenceNr { get; }

        /// <summary>
        /// Vector time used to track casual relationships between replicas.
        /// </summary>
        public VectorTime VectorTime { get; }

        public VersionClock(VectorTime vectorTime, long sequenceNr = 0L)
        {
            VectorTime = vectorTime;
            SequenceNr = sequenceNr;
        }

        public bool Equals(VersionClock other) => 
            SequenceNr == other.SequenceNr && VectorTime == other.VectorTime;

        public override bool Equals(object obj) => obj is VersionClock && Equals((VersionClock) obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (SequenceNr.GetHashCode() * 397) ^ VectorTime.GetHashCode();
            }
        }

        public override string ToString() => $"VersionClock({SequenceNr}, {VectorTime})";

        public int CompareTo(VersionClock other)
        {
            var cmp = VectorTime.PartiallyCompareTo(other.VectorTime);
            return cmp ?? SequenceNr.CompareTo(other.SequenceNr);
        }

        public int CompareTo(object obj)
        {
            if (obj is VersionClock) return CompareTo((VersionClock)obj);
            throw new ArgumentException($"Cannot compare VersionClock to '{obj?.GetType()}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VersionClock x, VersionClock y) => x.CompareTo(y) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VersionClock x, VersionClock y) => !(x == y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(VersionClock x, VersionClock y) => x.CompareTo(y) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(VersionClock x, VersionClock y) => x.CompareTo(y) == -1;

        public VersionClock Merge(VersionClock other) => 
            new VersionClock(VectorTime.Merge(other.VectorTime), Math.Max(SequenceNr, other.SequenceNr));
    }
}