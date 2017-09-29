using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;

namespace Calliope.Persistence
{
    public interface IPersistenceMessage { }
    public interface IPersistenceRequest : IPersistenceMessage { }
    public interface IPersistenceReply : IPersistenceMessage, IReply { }

    #region Write

    public struct Written : IEquatable<Written>
    {
        public readonly DurableEvent Event;

        public Written(DurableEvent @event)
        {
            Event = @event;
        }

        public override string ToString() => $"Written({Event})";

        public bool Equals(Written other)
        {
            return Equals(Event, other.Event);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Written && Equals((Written)obj);
        }

        public override int GetHashCode() => Event.GetHashCode();
    }

    public sealed class WriteBatch : IPersistenceRequest, IEquatable<WriteBatch>
    {
        public WriteBatch(ImmutableArray<Write> batch)
        {
            Batch = batch;
        }

        public ImmutableArray<Write> Batch { get; }

        public bool Equals(WriteBatch other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Batch.SequenceEqual(other.Batch);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is WriteBatch && Equals((WriteBatch)obj);
        }

        public override int GetHashCode() => Batch.GetHashCode();

        public override string ToString() => $"WriteBatch({string.Join(", ", Batch)})";
    }

    public sealed class WriteBatchComplete
    {
        public static readonly WriteBatchComplete Instance = new WriteBatchComplete();
        private WriteBatchComplete() { }
    }

    public sealed class Write : IPersistenceRequest, IReplyable<WriteReply>, IEquatable<Write>
    {
        public ImmutableArray<DurableEvent> Events { get; }
        public IActorRef ReplyTo { get; }
        public int CorrelationId { get; }

        public Write(ImmutableArray<DurableEvent> events, IActorRef replyTo, int correlationId)
        {
            Events = events;
            ReplyTo = replyTo;
            CorrelationId = correlationId;
        }

        public bool Equals(Write other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo) && Events.SequenceEqual(other.Events);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Write && Equals((Write)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Events.GetHashCode();
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CorrelationId;
                return hashCode;
            }
        }

        public override string ToString() =>
            $"Write(correlationId:{CorrelationId}, replyTo:{ReplyTo}, events: [{string.Join(", ", Events)}])";
    }

    public sealed class WriteReply : IPersistenceReply, IEquatable<WriteReply>
    {
        public ImmutableArray<DurableEvent> Events { get; }
        public int CorrelationId { get; }
        public Exception Cause { get; }

        public bool IsSuccess => Cause == null;
        public bool IsFailure => Cause != null;

        public WriteReply(ImmutableArray<DurableEvent> events, int correlationId, Exception cause = null)
        {
            Events = events;
            CorrelationId = correlationId;
            Cause = cause;
        }

        public bool Equals(WriteReply other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(Cause, other.Cause) && Events.SequenceEqual(other.Events);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is WriteReply && Equals((WriteReply)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Events.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (Cause != null ? Cause.GetHashCode() : 0);
                return hashCode;
            }
        }
        public override string ToString() =>
            $"WriteReply{(IsSuccess ? "Succeed" : "Failed")}(correlationId:{CorrelationId}, events: [{string.Join(", ", Events)}]{(IsFailure ? "cause: " + Cause : "")})";
    }

    #endregion

    #region Replay

    public sealed class Replay : IPersistenceRequest, IEquatable<Replay>, IReplyable<ReplayResult>
    {
        public long From { get; }
        public int Max { get; }
        public int CorrelationId { get; }
        public string EventStreamId { get; }
        public IActorRef Subscriber { get; }
        public IActorRef ReplyTo { get; }

        public Replay(long @from, int max, int correlationId, string eventStreamId, IActorRef subscriber, IActorRef replyTo)
        {
            From = @from;
            Max = max;
            CorrelationId = correlationId;
            EventStreamId = eventStreamId;
            ReplyTo = replyTo;
            Subscriber = subscriber;
        }

        public bool Equals(Replay other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return From == other.From
                && Max == other.Max
                && CorrelationId == other.CorrelationId
                && string.Equals(EventStreamId, other.EventStreamId)
                && Equals(Subscriber, other.Subscriber)
                && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Replay && Equals((Replay)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = From.GetHashCode();
                hashCode = (hashCode * 397) ^ Max;
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (EventStreamId != null ? EventStreamId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Subscriber != null ? Subscriber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() =>
            $"Replay(streamId: {EventStreamId}, from:{From}, max:{Max}, subscriber: {Subscriber}, correlationId:{CorrelationId}, replyTo:{ReplyTo})";
    }

    public abstract class ReplayResult : IPersistenceReply
    {
        public long Progress { get; }
        public int CorrelationId { get; }

        protected ReplayResult(long progress, int correlationId)
        {
            Progress = progress;
            CorrelationId = correlationId;
        }
    }

    public sealed class ReplaySuccess : ReplayResult, IEquatable<ReplaySuccess>
    {
        public ImmutableArray<DurableEvent> Events { get; }

        public ReplaySuccess(ImmutableArray<DurableEvent> events, long progress, int correlationId)
            : base(progress, correlationId)
        {
            Events = events;
        }

        public bool Equals(ReplaySuccess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId
                && Progress == other.Progress
                && Events.SequenceEqual(other.Events);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ReplaySuccess && Equals((ReplaySuccess)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CorrelationId.GetHashCode();
                hashCode = (hashCode * 397) ^ Progress.GetHashCode();
                hashCode = (hashCode * 397) ^ Events.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString() =>
            $"ReplaySuccess(correlationId: {CorrelationId}, progress: {Progress}, events: [{string.Join(", ", Events)}])";
    }

    public sealed class ReplayFailure : ReplayResult, IEquatable<ReplayFailure>
    {
        public Exception Cause { get; }

        public ReplayFailure(long progress, int correlationId, Exception cause) : base(progress, correlationId)
        {
            Cause = cause;
        }

        public bool Equals(ReplayFailure other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId
                && Progress == other.Progress
                && Equals(Cause, other.Cause);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ReplayFailure && Equals((ReplayFailure)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CorrelationId.GetHashCode();
                hashCode = (hashCode * 397) ^ Progress.GetHashCode();
                hashCode = (hashCode * 397) ^ Cause?.GetHashCode() ?? 0;
                return hashCode;
            }
        }

        public override string ToString() =>
            $"ReplayFailure(correlationId: {CorrelationId}, progress: {Progress}, cause: [{string.Join(", ", Cause)}])";
    }

    public sealed class ReplayRetry : IEquatable<ReplayRetry>
    {
        public ReplayRetry(long progress)
        {
            Progress = progress;
        }

        public long Progress { get; }

        public bool Equals(ReplayRetry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Progress == other.Progress;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ReplayRetry && Equals((ReplayRetry)obj);
        }

        public override int GetHashCode()
        {
            return Progress.GetHashCode();
        }

        public override string ToString() => $"ReplayRetry({Progress})";
    }

    #endregion

    #region Delete

    public sealed class Delete : IPersistenceRequest, IReplyable<DeleteReply>, IEquatable<Delete>
    {
        public long To { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }
        public ImmutableHashSet<string> RemoteIds { get; }

        public Delete(long to, ImmutableHashSet<string> remoteIds, int correlationId, IActorRef replyTo)
        {
            To = to;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
            RemoteIds = remoteIds;
        }

        public bool Equals(Delete other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return To == other.To && CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo) && Equals(RemoteIds, other.RemoteIds);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Delete && Equals((Delete)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = To.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RemoteIds != null ? RemoteIds.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() =>
            $"Delete(to: {To}, correlationId: {CorrelationId}, replyTo: {ReplyTo}, remoteReplicas: [{string.Join(", ", RemoteIds)}])";
    }

    public abstract class DeleteReply : IPersistenceReply
    {
        protected DeleteReply(int correlationId)
        {
            CorrelationId = correlationId;
        }

        public int CorrelationId { get; }
    }

    public sealed class DeleteSuccess : DeleteReply, IEquatable<DeleteSuccess>
    {
        public long To { get; }

        public DeleteSuccess(long to, int correlationId) : base(correlationId)
        {
            To = to;
        }

        public bool Equals(DeleteSuccess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return To == other.To && CorrelationId == other.CorrelationId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals((DeleteSuccess)obj);
        }

        public override int GetHashCode() => To.GetHashCode() ^ CorrelationId;

        public override string ToString() => $"DeleteSuccess(to: {To}, correlationId: {CorrelationId})";
    }

    public sealed class DeleteFailure : DeleteReply, IEquatable<DeleteFailure>
    {
        public Exception Cause { get; }

        public DeleteFailure(Exception cause, int correlationId)
            : base(correlationId)
        {
            Cause = cause;
        }

        public bool Equals(DeleteFailure other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(Cause, other.Cause);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals((DeleteFailure)obj);
        }

        public override int GetHashCode() => (Cause?.GetHashCode() ?? 0) ^ CorrelationId;

        public override string ToString() => $"DeleteFailure(correlationId: {CorrelationId}, cause: {Cause})";
    }

    #endregion

    #region SaveSnapshot 

    public sealed class SaveSnapshot : IPersistenceRequest, IReplyable<SaveSnapshotReply>, IEquatable<SaveSnapshot>
    {
        public Snapshot Snapshot { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public SaveSnapshot(Snapshot snapshot, int correlationId, IActorRef replyTo)
        {
            Snapshot = snapshot;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(SaveSnapshot other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Snapshot, other.Snapshot) && CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is SaveSnapshot && Equals((SaveSnapshot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Snapshot != null ? Snapshot.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() => $"SaveSnapshot({Snapshot}, correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public abstract class SaveSnapshotReply : IPersistenceReply
    {
        public int CorrelationId { get; }

        protected SaveSnapshotReply(int correlationId)
        {
            CorrelationId = correlationId;
        }
    }

    public sealed class SaveSnapshotSuccess : SaveSnapshotReply, IEquatable<SaveSnapshotSuccess>
    {
        public SnapshotMetadata Metadata { get; }

        public SaveSnapshotSuccess(SnapshotMetadata metadata, int correlationId) : base(correlationId)
        {
            Metadata = metadata;
        }

        public bool Equals(SaveSnapshotSuccess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Metadata.Equals(other.Metadata) && CorrelationId == other.CorrelationId;
        }

        public override bool Equals(object obj) => obj is SaveSnapshotSuccess && Equals((SaveSnapshotSuccess)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Metadata.GetHashCode() * 397) ^ CorrelationId;
            }
        }

        public override string ToString() => $"SaveSnapshotSuccess({Metadata}, correlationId: {CorrelationId})";
    }

    public sealed class SaveSnapshotFailure : SaveSnapshotReply, IEquatable<SaveSnapshotFailure>
    {
        public Exception Cause { get; }

        public SaveSnapshotFailure(Exception cause, int correlationId) : base(correlationId)
        {
            Cause = cause;
        }

        public bool Equals(SaveSnapshotFailure other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Cause.Equals(other.Cause) && CorrelationId == other.CorrelationId;
        }

        public override bool Equals(object obj) => obj is SaveSnapshotFailure && Equals((SaveSnapshotFailure)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Cause.GetHashCode() * 397) ^ CorrelationId;
            }
        }

        public override string ToString() => $"SaveSnapshotFailure({Cause}, correlationId: {CorrelationId})";
    }

    #endregion

    #region LoadSnapshot

    public sealed class LoadSnapshot : IPersistenceRequest, IReplyable<LoadSnapshotReply>, IEquatable<LoadSnapshot>
    {
        public string StreamId { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public LoadSnapshot(string streamId, int correlationId, IActorRef replyTo)
        {
            StreamId = streamId;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(LoadSnapshot other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(StreamId, other.StreamId) && CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LoadSnapshot && Equals((LoadSnapshot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (StreamId != null ? StreamId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() => $"LoadSnapshot(streamId: {StreamId}, correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public sealed class LoadSnapshotReply : IPersistenceReply, IEquatable<LoadSnapshotReply>
    {
        public Snapshot Snapshot { get; }
        public int CorrelationId { get; }
        public Exception Cause { get; }

        public bool IsSuccess => Cause == null;
        public bool IsFailure => Cause != null;

        public LoadSnapshotReply(Snapshot snapshot, int correlationId, Exception cause = null)
        {
            Snapshot = snapshot;
            CorrelationId = correlationId;
            Cause = cause;
        }

        public bool Equals(LoadSnapshotReply other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Snapshot.Equals(other.Snapshot) && CorrelationId == other.CorrelationId && Equals(Cause, other.Cause);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LoadSnapshotReply && Equals((LoadSnapshotReply)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Snapshot.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrelationId.GetHashCode();
                hashCode = (hashCode * 397) ^ (Cause != null ? Cause.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() =>
            $"LoadSnapshot{(IsSuccess ? "Success" : "Failed")}({Snapshot}, correlationId: {CorrelationId}{(IsFailure ? ", cause: " + Cause : "")})";
    }

    #endregion

    #region DeleteSnapshot

    public sealed class DeleteSnapshot : IPersistenceRequest, IReplyable<DeleteSnapshotReply>, IEquatable<DeleteSnapshot>
    {
        public long From { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public DeleteSnapshot(long @from, int correlationId, IActorRef replyTo)
        {
            From = @from;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(DeleteSnapshot other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return From == other.From && CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is DeleteSnapshot && Equals((DeleteSnapshot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = From.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() => $"DeleteSnapshot(from: {From}, correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public sealed class DeleteSnapshotReply : IPersistenceReply, IEquatable<DeleteSnapshotReply>
    {
        public Exception Cause { get; }
        public int CorrelationId { get; }

        public bool IsSuccess => Cause == null;
        public bool IsFailure => Cause != null;

        public DeleteSnapshotReply(int correlationId, Exception cause = null)
        {
            Cause = cause;
            CorrelationId = correlationId;
        }

        public bool Equals(DeleteSnapshotReply other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Cause, other.Cause) && CorrelationId == other.CorrelationId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is DeleteSnapshotReply && Equals((DeleteSnapshotReply)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Cause != null ? Cause.GetHashCode() : 0) * 397) ^ CorrelationId;
            }
        }

        public override string ToString() =>
            $"DeleteSnapshot{(IsSuccess ? "Succeed" : "Failed")}(correlationId: {CorrelationId}, cause: {Cause})";
    }

    #endregion
}