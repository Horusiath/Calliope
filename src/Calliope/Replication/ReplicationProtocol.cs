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
    using ReplicaId = Int32;

    /// <summary>
    /// An invitation message send between <see cref="ReplicatorActorActor{T}"/> actors to establish
    /// edges in replication graph.
    /// </summary>
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
            return obj is PendingAck<T> ack && Equals(ack);
        }

        public override int GetHashCode() => hashCode;

        private int HashCode()
        {
            unchecked
            {
                var hashCode = Target.GetHashCode();
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

    /// <summary>
    /// A subscription request send by an actor identified by <see cref="Ref"/>
    /// in order to subscribe to a replicated topic. That actor will receive
    /// messages wrapped into <see cref="Versioned{T}"/> wrapper.
    /// 
    /// If <see cref="Ack"/> object was specified, it will be returned by replicator
    /// back to <see cref="Ref"/>, once a subscription will complete successfully.
    /// </summary>
    public sealed class Subscribe<T>
    {
        public Subscribe(IActorRef @ref, object ack = null)
        {
            Ref = @ref;
            Ack = ack;
        }

        public IActorRef Ref { get; }
        public object Ack { get; }
    }

    /// <summary>
    /// An unsubscribe request for a previously <see cref="Subscribe"/>d actor
    /// identified by <see cref="Ref"/> reference. It's not necessary to explicitly
    /// unsubscribe actors: replicator will watch them and unsubscribe them automatically
    /// on termination.
    /// 
    /// If <see cref="Ack"/> object was specified, it will be returned by replicator
    /// back to <see cref="Ref"/>, once an unsubscribe will complete. It won't be send
    /// if actor has been automatically unsubscribed due to termination.
    /// </summary>
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
        /// <summary>
        /// Replica id of the replicator, which has initialized this message to be broadcasted.
        /// </summary>
        public ReplicaId Origin { get; }

        /// <summary>
        /// Replica id of the last replicator, which has send current message. 
        /// Effectively more performant equivalent of <see cref="IActorContext.Sender"/>.
        /// </summary>
        public ReplicaId LastSeenBy { get; }
        public Versioned<T> Versioned { get; }

        public Send(ReplicaId origin, ReplicaId lastSeenBy, Versioned<T> versioned)
        {
            Origin = origin;
            LastSeenBy = lastSeenBy;
            Versioned = versioned;
        }

        public Send<T> WithLastSeenBy(ReplicaId lastSender) => new Send<T>(Origin, lastSender, Versioned);

        public bool Equals(Send<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Origin == other.Origin
                && LastSeenBy == other.LastSeenBy
                && Versioned.Equals(other.Versioned);
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
                hashCode = (hashCode * 397) ^ LastSeenBy.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString() => $"Send(origin: {Origin}, lastSeenBy: {LastSeenBy}, message: {Versioned.Message}, version:{Versioned.Version})";
    }

    public sealed class SendAck
    {
        public ReplicaId ReplicaId { get; }
        public VClock Version { get; }

        public SendAck(ReplicaId replicaId, VClock version)
        {
            ReplicaId = replicaId;
            Version = version;
        }
    }

    public sealed class Deliver<T>
    {
        public ReplicaId Origin { get; }
        public Versioned<T> Versioned { get; }

        public Deliver(ReplicaId origin, Versioned<T> versioned)
        {
            Origin = origin;
            Versioned = versioned;
        }
    }

    public sealed class Resend
    {
        public static Resend Instance { get; } = new Resend();
        private Resend() { }
    }
}