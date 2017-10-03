#region copyright
// -----------------------------------------------------------------------
//  <copyright file="Versioned.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;

namespace Calliope.Replication
{
    /// <summary>
    /// An envelope over generic message <typeparamref name="T"/>, that attaches a vector time to it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Versioned<T> : IPartiallyComparable<Versioned<T>>, IEquatable<Versioned<T>>
    {
        public VClock Version { get; }
        public T Message { get; }

        public Versioned(VClock version, T message)
        {
            Version = version;
            Message = message;
        }

        public int? PartiallyCompareTo(Versioned<T> other) => 
            Version.PartiallyCompareTo(other.Version);


        public bool Equals(Versioned<T> other) => 
            Version.Equals(other.Version) 
            && EqualityComparer<T>.Default.Equals(Message, other.Message);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Versioned<T> versioned && Equals(versioned);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Version.GetHashCode() * 397) ^ EqualityComparer<T>.Default.GetHashCode(Message);
            }
        }

        public override string ToString() => $"<message:{Message}, ver:{Version}>";
    }
}