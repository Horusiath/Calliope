using System;
using System.Collections.Generic;

namespace Calliope
{
    public struct Versioned<T> : IEquatable<Versioned<T>>, IConvergent<Versioned<T>>
    {
        public Version Version { get; }
        public T Value { get; }

        public Versioned(T value, Version version)
        {
            Version = version;
            Value = value;
        }

        public Versioned(T value) : this(value, Version.Zero) { }

        public bool Equals(Versioned<T> other)
        {
            return Version.Equals(other.Version) && Equals(Value, other.Value);
        }

        public Versioned<T> Merge(Versioned<T> other)
        {
            var vresult = Version.VectorTime.PartiallyCompareTo(other.Version.VectorTime);
            switch (vresult)
            {
                case 1: case 0: return this;
                case -1: return other;
                case null:  // concurrent update
                    switch (Version.SystemTime.CompareTo(other.Version.SystemTime))
                    {
                        case 1: return new Versioned<T>(this.Value, Version.Merge(other.Version));
                        case -1: return new Versioned<T>(other.Value, Version.Merge(other.Version));
                        case 0 when Equals(this.Value, other.Value): return new Versioned<T>(this.Value, Version.Merge(other.Version));
                        case 0: throw new ConcurrentUpdateException($"Tried to resolve concurrent update conflict, but the system timestamps were identical");
                        case var x: throw new NotSupportedException($"Version comparison unknown. Expected: 1 | 0 | -1, but was {x}");
                    }
                default: throw new NotSupportedException($"Version comparison unknown. Expected: 1 | 0 | -1 | null, but was {vresult}");
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Versioned<T> versioned && Equals(versioned);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Version.GetHashCode() * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value);
            }
        }
    }
}