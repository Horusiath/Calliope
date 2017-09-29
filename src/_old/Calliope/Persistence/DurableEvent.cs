using System;
using System.Runtime.CompilerServices;
using Calliope.Replication;

namespace Calliope.Persistence
{
    /// <summary>
    /// Data structure used to pass information about events between replicas and persistent storage.
    /// </summary>
    public abstract class DurableEvent : IEquatable<DurableEvent>
    {
        /// <summary>
        /// Event defined by the end user.
        /// </summary>
        protected object UntypedPayload { get; }

        /// <summary>
        /// Id of the replica that caused this event.
        /// </summary>
        public string ReplicaId { get; }

        /// <summary>
        /// Id of the globally-identified aggreagate stream, this event is associated with.
        /// </summary>
        public string StreamId { get; }

        /// <summary>
        /// VersionClock of an event. It can be tracked to achieve casual order of events.
        /// </summary>
        public Version Version { get; }

        protected DurableEvent(object payload, Version version, string streamId, string replicaId = null)
        {
            UntypedPayload = payload ?? throw new ArgumentNullException(nameof(payload));
            Version = version;
            StreamId = streamId ?? throw new ArgumentNullException(nameof(streamId));
            ReplicaId = replicaId;
        }

        /// <summary>
        /// Returns true if event didn't happen before or at the given <paramref name="time"/>
        /// and optionally passes provided replication <paramref name="filter"/>.
        /// </summary>
        public bool CanReplicate(VectorTime time, ReplicationFilter filter = null) =>
            !IsBefore(time) && (filter == null || filter(this));

        /// <summary>
        /// Returns true if current event happened before or at the given <paramref name="version"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBefore(VectorTime version) => this.Version.VectorTime <= version;

        public bool Equals(DurableEvent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(UntypedPayload, other.UntypedPayload)
                && string.Equals(ReplicaId, other.ReplicaId)
                && string.Equals(StreamId, other.StreamId)
                && Version.Equals(other.Version);
        }

        public override bool Equals(object obj) => obj is DurableEvent && Equals((DurableEvent)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (UntypedPayload != null ? UntypedPayload.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ReplicaId.GetHashCode());
                hashCode = (hashCode * 397) ^ (StreamId.GetHashCode());
                hashCode = (hashCode * 397) ^ Version.GetHashCode();
                return hashCode;
            }
        }
    }

    public sealed class DurableEvent<TPayload> : DurableEvent
    {
        public DurableEvent(object payload, string replicaId, string streamId, Version version)
            : base(payload, version, streamId, replicaId)
        {
        }

        public TPayload Payload => (TPayload)UntypedPayload;
    }
}