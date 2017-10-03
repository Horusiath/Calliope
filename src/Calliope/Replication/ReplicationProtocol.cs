#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ReplicationProtocol.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;

namespace Calliope.Replication
{
    using ReplicaId = Address;

    public static class Replicator
    {
        public sealed class Invitation
        {
            public ReplicaId ReplicaId { get; }
            public IActorRef ReplicatorRef { get; }

            public Invitation(ReplicaId replicaId, IActorRef replicatorRef)
            {
                ReplicaId = replicaId;
                ReplicatorRef = replicatorRef;
            }
        }
        
        public sealed class PendingAck<T> : IEquatable<PendingAck<T>>
        {
            private readonly int hashCode;
            public ReplicaId Target { get; }
            public Versioned<T> Versioned { get; }
            public DateTime Timestamp { get; }
            public ImmutableHashSet<ReplicaId> Members { get; }

            public PendingAck(ReplicaId target, Versioned<T> versioned, DateTime timestamp, ImmutableHashSet<ReplicaId> members)
            {
                Target = target;
                Versioned = versioned;
                Timestamp = timestamp;
                Members = members;

                hashCode = HashCode();
            }

            public bool Equals(PendingAck<T> other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Target, other.Target)
                    && Versioned.Equals(other.Versioned)
                    && Timestamp.Equals(other.Timestamp)
                    && Members.SetEquals(other.Members);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is PendingAck<T> && Equals((PendingAck<T>)obj);
            }

            public override int GetHashCode() => hashCode;

            private int HashCode()
            {
                unchecked
                {
                    var hashCode = (Target != null ? Target.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Versioned.GetHashCode();
                    hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                    foreach (var address in Members)
                    {
                        hashCode = (hashCode * 397) ^ address.GetHashCode();
                    }
                    return hashCode;
                }
            }

            public PendingAck<T> WithMembers(ImmutableHashSet<ReplicaId> members) => new PendingAck<T>(Target, Versioned, Timestamp, members);

            public PendingAck<T> WithTimestamp(DateTime timestamp) => new PendingAck<T>(Target, Versioned, timestamp, Members);
        }

        public sealed class StableReq
        {
            public StableReq(IEnumerable<VClock> versions)
            {
                Versions = versions;
            }

            public IEnumerable<VClock> Versions { get; }
        }

        public sealed class StableRep
        {
            public StableRep(IEnumerable<VClock> versions)
            {
                Versions = versions;
            }

            public IEnumerable<VClock> Versions { get; }
        }

        public sealed class Broadcast<T>
        {
            public Broadcast(T message)
            {
                Message = message;
            }

            public T Message { get; }
        }

        public sealed class Subscribe
        {
            public Subscribe(IActorRef @ref, object ack = null)
            {
                Ref = @ref;
                Ack = ack;
            }

            public IActorRef Ref { get; }
            public object Ack { get; }
        }

        public sealed class Unsubscribe
        {
            public Unsubscribe(IActorRef @ref, object ack = null)
            {
                Ref = @ref;
                Ack = ack;
            }

            public IActorRef Ref { get; }
            public object Ack { get; }
        }

        public sealed class Send<T> : IEquatable<Send<T>>
        {
            public ReplicaId Origin { get; }
            public Versioned<T> Versioned { get; }
            public ImmutableHashSet<ReplicaId> SeenBy { get; }

            public Send(ReplicaId origin, Versioned<T> versioned, ImmutableHashSet<ReplicaId> seenBy)
            {
                Origin = origin;
                Versioned = versioned;
                SeenBy = seenBy;
            }

            public Send<T> UpdateSeenBy(ReplicaId address) => new Send<T>(Origin, Versioned, SeenBy.Add(address));

            public bool Equals(Send<T> other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Origin == other.Origin
                    && Versioned.Equals(other.Versioned)
                    && SeenBy.SequenceEqual(other.SeenBy);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Send<T> send && Equals(send);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Origin.GetHashCode() * 397) ^ Versioned.GetHashCode();
                    foreach (var address in SeenBy)
                    {
                        hashCode = (hashCode * 397) ^ address.GetHashCode();
                    }
                    return hashCode;
                }
            }

            public override string ToString() => $"Send(origin: {Origin}, message: {Versioned.Message}, version:{Versioned.Version}, seenBy:{string.Join(", ", SeenBy)})";
        }

        public sealed class SendAck
        {
            public SendAck(VClock version)
            {
                Version = version;
            }

            public VClock Version { get; }
        }

        public sealed class Deliver<T>
        {
            public Versioned<T> Versioned { get; }

            public Deliver(Versioned<T> versioned)
            {
                Versioned = versioned;
            }
        }

        public sealed class Resend
        {
            public static Resend Instance { get; } = new Resend();
            private Resend() { }
        }
    }
}