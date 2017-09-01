using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Calliope
{
    /// <summary>
    /// Version combines concurrent time recognized as <see cref="VectorTime"/>
    /// with backtrack to <see cref="SystemTime"/> when concurrent changes has 
    /// been recognized.
    /// </summary>
    public struct Version : IEquatable<Version>, IComparable<Version>, IComparable, IConvergent<Version>
    {
        /// <summary>
        /// Monotonically increasing number of local replica instance.
        /// </summary>
        public DateTime SystemTime { get; }

        /// <summary>
        /// Vector time used to track casual relationships between replicas.
        /// </summary>
        public VectorTime VectorTime { get; }

        public Version(DateTime systemTime, VectorTime vectorTime)
        {
            SystemTime = systemTime;
            VectorTime = vectorTime;
        }

        [Pure]
        public bool Equals(Version other) => SystemTime == other.SystemTime && VectorTime == other.VectorTime;

        [Pure]
        public override bool Equals(object obj) => obj is Version version && Equals(version);

        public override int GetHashCode()
        {
            unchecked
            {
                return (SystemTime.GetHashCode() * 397) ^ VectorTime.GetHashCode();
            }
        }

        public override string ToString() => $"VersionClock({SystemTime:O}, {VectorTime})";

        [Pure]
        public int CompareTo(Version other)
        {
            var cmp = VectorTime.PartiallyCompareTo(other.VectorTime);
            return cmp ?? SystemTime.CompareTo(other.SystemTime);
        }
        
        public int CompareTo(object obj)
        {
            if (obj is Version version) return CompareTo(version);
            throw new ArgumentException($"Cannot compare VersionClock to '{obj?.GetType()}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Version x, Version y) => x.CompareTo(y) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Version x, Version y) => !(x == y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Version x, Version y) => x.CompareTo(y) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Version x, Version y) => x.CompareTo(y) == -1;

        public Version Merge(Version other)
        {
            var ticks = Math.Max(SystemTime.Ticks, other.SystemTime.Ticks);
            return new Version(new DateTime(ticks), VectorTime.Merge(other.VectorTime));
        }
    }
}