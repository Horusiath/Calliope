using System;

namespace Calliope.Persistence
{
    public struct SnapshotMetadata : IEquatable<SnapshotMetadata>
    {
        /// <summary>
        /// Identifier of a replica that related state/snapshot represents.
        /// </summary>
        public readonly string ReplicaId;

        /// <summary>
        /// The highest event sequence nr covered by this snapshot.
        /// </summary>
        public readonly long SequenceNr;

        public SnapshotMetadata(string replicaId, long sequenceNr)
        {
            ReplicaId = replicaId;
            SequenceNr = sequenceNr;
        }

        public bool Equals(SnapshotMetadata other)
        {
            return string.Equals(ReplicaId, other.ReplicaId) && SequenceNr == other.SequenceNr;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SnapshotMetadata && Equals((SnapshotMetadata)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ReplicaId.GetHashCode() * 397) ^ SequenceNr.GetHashCode();
            }
        }

        public override string ToString() => $"SnapshotMetadata({ReplicaId}:{SequenceNr})";
    }

    public sealed class Snapshot : IEquatable<Snapshot>
    {
        public readonly object Payload;
        public readonly SnapshotMetadata Metadata;
        public readonly Version Version;

        public Snapshot(object payload, SnapshotMetadata metadata, Version version)
        {
            Payload = payload;
            Metadata = metadata;
            Version = version;
        }

        public bool Equals(Snapshot other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Payload, other.Payload) && Metadata.Equals(other.Metadata) && Version.Equals(other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Snapshot && Equals((Snapshot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Payload != null ? Payload.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Metadata.GetHashCode();
                hashCode = (hashCode * 397) ^ Version.GetHashCode();
                return hashCode;
            }
        }
    }
}