using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Akka.Actor;
using Calliope.Persistence;

namespace Calliope.Replication
{
    public interface IReplicationMessage { }
    public interface IReplicationRequest : IReplicationMessage { }
    public interface IReplicationReply : IReplicationMessage, IReply { }

    #region Replication endpoint info 

    public struct ReplicationEndpointInfo : IEquatable<ReplicationEndpointInfo>
    {
        public static readonly ReplicationEndpointInfo None = new ReplicationEndpointInfo(ReplicationEndpoint.None, ImmutableDictionary<string, long>.Empty);

        public ReplicationEndpoint Endpoint { get; }
        public ImmutableDictionary<string, long> JournalSequenceNumbers { get; }

        public ReplicationEndpointInfo(ReplicationEndpoint endpoint, ImmutableDictionary<string, long> journalSequenceNumbers)
        {
            Endpoint = endpoint;
            JournalSequenceNumbers = journalSequenceNumbers;
        }

        public bool Equals(ReplicationEndpointInfo other)
        {
            if (!Endpoint.Equals(other.Endpoint)) return false;
            return JournalSequenceNumbers.DictionaryEquals(other.JournalSequenceNumbers);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ReplicationEndpointInfo && Equals((ReplicationEndpointInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Endpoint.GetHashCode() * 397 ^ JournalSequenceNumbers.DictionaryHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder("ReplicationEndpointInfo(").Append(Endpoint).Append(", {");
            foreach (var entry in JournalSequenceNumbers)
            {
                sb.Append(entry.Key).Append(": ").Append(entry.Value).AppendLine();
            }
            return sb.Append("})").ToString();
        }
    }

    public sealed class GetReplicationEndpoint : IReplicationRequest, IReplyable<GetReplicationEndpointReply>, IEquatable<GetReplicationEndpoint>
    {
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public GetReplicationEndpoint(int correlationId, IActorRef replyTo)
        {
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(GetReplicationEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is GetReplicationEndpoint && Equals((GetReplicationEndpoint)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CorrelationId * 397) ^ (ReplyTo?.GetHashCode() ?? 0);
            }
        }

        public override string ToString() => $"GetReplicationEndpoint(correlationId:{CorrelationId}, replyTo:{ReplyTo})";
    }

    public class GetReplicationEndpointReply : IReplicationReply, IEquatable<GetReplicationEndpointReply>
    {
        public ReplicationEndpointInfo Info { get; }
        public int CorrelationId { get; }

        public GetReplicationEndpointReply(ReplicationEndpointInfo info, int correlationId)
        {
            Info = info;
            CorrelationId = correlationId;
        }

        public bool Equals(GetReplicationEndpointReply other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Info.Equals(other.Info) && CorrelationId == other.CorrelationId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GetReplicationEndpointReply)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Info.GetHashCode() * 397) ^ CorrelationId;
            }
        }

        public override string ToString() => $"GetReplicationEndpointReply({Info}, correlationId: {CorrelationId})";
    }

    #endregion

    #region Synchronize replication progress

    public abstract class SyncReplicationException : Exception
    {
        protected SyncReplicationException(string message) : base(message) { }
        protected SyncReplicationException(string message, Exception innerException) : base(message, innerException) { }
        protected SyncReplicationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public abstract class SyncReplicationSourceException : SyncReplicationException
    {
        protected SyncReplicationSourceException(string message) : base("Failure when updating local replication progress: " + message) { }
        protected SyncReplicationSourceException(string message, Exception innerException) : base("Failure when updating local replication progress: " + message, innerException) { }
        protected SyncReplicationSourceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public sealed class SyncReplication : IReplicationRequest, IReplyable<SyncReplicationReply>, IEquatable<SyncReplication>
    {
        public ReplicationEndpointInfo Info { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public SyncReplication(ReplicationEndpointInfo info, int correlationId, IActorRef replyTo)
        {
            Info = info;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(SyncReplication other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Info.Equals(other.Info) && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj)
        {
            return obj is SyncReplication && Equals((SyncReplication)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Info.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() =>
            $"SyncReplication({Info}, correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public sealed class SyncReplicationReply : IReplicationReply, IEquatable<SyncReplicationReply>
    {
        public ReplicationEndpointInfo Info { get; }
        public SyncReplicationException Cause { get; }
        public int CorrelationId { get; }

        public bool IsSuccess => Cause == null;
        public bool IsFailure => Cause != null;

        public SyncReplicationReply(ReplicationEndpointInfo info, int correlationId, SyncReplicationException cause = null)
        {
            Info = info;
            Cause = cause;
            CorrelationId = correlationId;
        }

        public bool Equals(SyncReplicationReply other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Info.Equals(other.Info) && Equals(Cause, other.Cause);
        }

        public override bool Equals(object obj)
        {
            return Equals((SyncReplicationReply)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Info.GetHashCode();
                hashCode = (hashCode * 397) ^ (Cause?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ CorrelationId;
                return hashCode;
            }
        }

        public override string ToString() =>
            IsSuccess
            ? $"SyncReplicationSuccess({Info}, correlationId: {CorrelationId})"
            : $"SyncReplicationFailure(correlationId: {CorrelationId}, cause: {Cause})";
    }

    #endregion

    public sealed class ReplicationAvailable : IReplicationMessage
    {
        public static readonly ReplicationAvailable Instance = new ReplicationAvailable();
        private ReplicationAvailable() { }
    }

    #region Get replication progress

    public sealed class GetReplicationProgresses : IReplicationRequest, IReplyable<GetReplicationProgressesReply>, IEquatable<GetReplicationProgresses>
    {
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public GetReplicationProgresses(int correlationId, IActorRef replyTo)
        {
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(GetReplicationProgresses other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj) => obj is GetReplicationProgresses && Equals((GetReplicationProgresses)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (CorrelationId * 397) ^ (ReplyTo?.GetHashCode() ?? 0);
            }
        }

        public override string ToString() => $"GetReplicationProgresses(correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public sealed class GetReplicationProgressesReply : IReplicationReply, IEquatable<GetReplicationProgressesReply>
    {
        public int CorrelationId { get; }
        public ImmutableDictionary<string, long> Progresses { get; }
        public Exception Cause { get; }

        public bool IsSuccess => Cause == null;
        public bool IsFailure => Cause != null;

        public GetReplicationProgressesReply(int correlationId, ImmutableDictionary<string, long> progresses, Exception cause = null)
        {
            CorrelationId = correlationId;
            Progresses = progresses;
            Cause = cause;
        }

        public bool Equals(GetReplicationProgressesReply other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(Cause, other.Cause) && Progresses.DictionaryEquals(other.Progresses);
        }

        public override bool Equals(object obj) => obj is GetReplicationProgressesReply && Equals((GetReplicationProgressesReply)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CorrelationId;
                hashCode = (hashCode * 397) ^ Progresses.DictionaryHashCode();
                hashCode = (hashCode * 397) ^ (Cause != null ? Cause.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() => IsSuccess
            ? $"GetReplicationProgressesSuccess({Progresses}, correlationId: {CorrelationId})"
            : $"GetReplicationProgressesFailure(correlationId: {CorrelationId}, cause: {Cause})";
    }

    public sealed class GetReplicationProgress : IReplicationRequest, IReplyable<GetReplicationProgressReply>, IEquatable<GetReplicationProgress>
    {
        public string SourceId { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public GetReplicationProgress(string sourceId, int correlationId, IActorRef replyTo)
        {
            SourceId = sourceId;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(GetReplicationProgress other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(SourceId, other.SourceId) && CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj) => obj is GetReplicationProgress && Equals((GetReplicationProgress)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (SourceId != null ? SourceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() => $"GetReplicationProgress({SourceId}, correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public abstract class GetReplicationProgressReply : IReplicationReply
    {
        protected GetReplicationProgressReply(int correlationId)
        {
            CorrelationId = correlationId;
        }

        public int CorrelationId { get; }
        public abstract bool IsSuccess { get; }
        public abstract bool IsFailure { get; }
    }

    public sealed class GetReplicationProgressSuccess : GetReplicationProgressReply, IEquatable<GetReplicationProgressSuccess>
    {
        public string SourceId { get; }
        public long Progress { get; }
        public VectorTime TargetVersion { get; }

        public GetReplicationProgressSuccess(int correlationId, string sourceId, long progress, VectorTime targetVersion) : base(correlationId)
        {
            SourceId = sourceId;
            Progress = progress;
            TargetVersion = targetVersion;
        }

        public override bool IsSuccess => true;
        public override bool IsFailure => false;

        public bool Equals(GetReplicationProgressSuccess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && string.Equals(SourceId, other.SourceId) && Progress == other.Progress && TargetVersion.Equals(other.TargetVersion);
        }

        public override bool Equals(object obj) => obj is GetReplicationProgressSuccess && Equals((GetReplicationProgressSuccess)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CorrelationId.GetHashCode();
                hashCode = (hashCode * 397) ^ (SourceId?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Progress.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetVersion.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString() => $"GetReplicationProgressSuccess({SourceId}: {Progress}, version: {TargetVersion}, correlationId: {CorrelationId})";
    }

    public sealed class GetReplicationProgressFailure : GetReplicationProgressReply, IEquatable<GetReplicationProgressFailure>
    {
        public Exception Cause { get; }

        public GetReplicationProgressFailure(int correlationId, Exception cause) : base(correlationId)
        {
            Cause = cause;
        }

        public override bool IsSuccess => false;
        public override bool IsFailure => true;

        public bool Equals(GetReplicationProgressFailure other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(Cause, other.Cause);
        }

        public override bool Equals(object obj) => obj is GetReplicationProgressFailure && Equals((GetReplicationProgressFailure)obj);

        public override int GetHashCode()
        {
            return (CorrelationId.GetHashCode() * 397) ^ (Cause?.GetHashCode() ?? 0);
        }

        public override string ToString() => $"GetReplicationProgressFailure(correlationId: {CorrelationId}, cause: {Cause})";
    }

    #endregion

    #region Set replication read

    internal sealed class ReplicationDue : IReplicationRequest
    {
        public static readonly ReplicationDue Instance = new ReplicationDue();

        private ReplicationDue()
        {
        }
    }
    public sealed class SetReplicationProgress : IReplicationRequest, IReplyable<SetReplicationProgressReply>, IEquatable<SetReplicationProgress>
    {
        public string SourceId { get; }
        public long Progress { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public SetReplicationProgress(string sourceId, long progress, int correlationId, IActorRef replyTo)
        {
            SourceId = sourceId;
            Progress = progress;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(SetReplicationProgress other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(SourceId, other.SourceId) && Progress == other.Progress && CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj) => obj is SetReplicationProgress && Equals((SetReplicationProgress)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (SourceId != null ? SourceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Progress.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString() => $"SetReplicationProgress({SourceId}: {Progress}, correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public abstract class SetReplicationProgressReply : IReplicationReply
    {
        protected SetReplicationProgressReply(int correlationId)
        {
            CorrelationId = correlationId;
        }

        public int CorrelationId { get; }
        public abstract bool IsSuccess { get; }
        public abstract bool IsFailure { get; }
    }

    public sealed class SetReplicationProgressSuccess : SetReplicationProgressReply, IEquatable<SetReplicationProgressSuccess>
    {
        public string SourceId { get; }
        public long StoredProgress { get; }

        public SetReplicationProgressSuccess(int correlationId, string sourceId, long storedProgress) : base(correlationId)
        {
            SourceId = sourceId;
            StoredProgress = storedProgress;
        }

        public override bool IsSuccess => true;
        public override bool IsFailure => false;

        public bool Equals(SetReplicationProgressSuccess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && string.Equals(SourceId, other.SourceId) && StoredProgress == other.StoredProgress;
        }

        public override bool Equals(object obj) => obj is SetReplicationProgressSuccess && Equals((SetReplicationProgressSuccess)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (CorrelationId * 397) ^ ((SourceId?.GetHashCode() ?? 0) * 397) ^ StoredProgress.GetHashCode();
            }
        }

        public override string ToString() => $"SetReplicationProgressSuccess({SourceId}: {StoredProgress}, correlationId: {CorrelationId})";
    }

    public sealed class SetReplicationProgressFailure : SetReplicationProgressReply, IEquatable<SetReplicationProgressFailure>
    {
        public Exception Cause { get; }
        public override bool IsSuccess => false;
        public override bool IsFailure => true;

        public SetReplicationProgressFailure(int correlationId, Exception cause) : base(correlationId)
        {
            Cause = cause;
        }

        public bool Equals(SetReplicationProgressFailure other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(Cause, other.Cause);
        }

        public override bool Equals(object obj) => obj is SetReplicationProgressFailure && Equals((SetReplicationProgressFailure)obj);

        public override int GetHashCode() => (CorrelationId.GetHashCode() * 397) ^ (Cause?.GetHashCode() ?? 0);

        public override string ToString() => $"SetReplicationProgressFailure(correlationId: {CorrelationId}, cause: {Cause})";
    }

    #endregion

    #region Replication read

    public sealed class ReplicationReadEnvelope
    {
        public ReplicationRead Payload { get; }
        public string JournalName { get; }

        public ReplicationReadEnvelope(ReplicationRead payload, string journalName)
        {
            Payload = payload;
            JournalName = journalName;
        }
    }

    public sealed class ReplicationRead : IReplicationRequest, IEquatable<ReplicationRead>, IReplyable<ReplicationReadReply>
    {
        public long From { get; }
        public int Max { get; }
        public int ScanLimit { get; }
        public string TargetId { get; }
        public ReplicationFilter Filter { get; }
        public IActorRef Replicator { get; }
        public VectorTime TargetVersion { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public ReplicationRead(long @from, int max, int scanLimit, string targetId, ReplicationFilter filter, IActorRef replicator, VectorTime targetVersion, int correlationId, IActorRef replyTo)
        {
            From = @from;
            Max = max;
            ScanLimit = scanLimit;
            TargetId = targetId;
            Filter = filter;
            Replicator = replicator;
            TargetVersion = targetVersion;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(ReplicationRead other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return From == other.From
                && Max == other.Max
                && ScanLimit == other.ScanLimit
                && string.Equals(TargetId, other.TargetId)
                && Equals(Filter, other.Filter)
                && Equals(Replicator, other.Replicator)
                && TargetVersion.Equals(other.TargetVersion)
                && CorrelationId == other.CorrelationId
                && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj) => obj is ReplicationRead && Equals((ReplicationRead)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = From.GetHashCode();
                hashCode = (hashCode * 397) ^ Max;
                hashCode = (hashCode * 397) ^ ScanLimit;
                hashCode = (hashCode * 397) ^ (TargetId != null ? TargetId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Filter != null ? Filter.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Replicator != null ? Replicator.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TargetVersion.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public abstract class ReplicationReadReply : IReplicationReply
    {
        public string TargetId { get; }
        public int CorrelationId { get; }
        public abstract bool IsSuccess { get; }
        public abstract bool IsFailure { get; }

        protected ReplicationReadReply(string targetId, int correlationId)
        {
            TargetId = targetId;
            CorrelationId = correlationId;
        }
    }

    public sealed class ReplicationReadSuccess : ReplicationReadReply, IEquatable<ReplicationReadSuccess>
    {
        public ImmutableArray<DurableEvent> Events { get; }
        public long From { get; }
        public long Progress { get; }
        public VectorTime SourceVersion { get; }

        public override bool IsSuccess => true;
        public override bool IsFailure => false;

        public ReplicationReadSuccess(int correlationId, ImmutableArray<DurableEvent> events, long @from, long progress, string targetId, VectorTime sourceVersion)
            : base(targetId, correlationId)
        {
            Events = events;
            From = @from;
            Progress = progress;
            SourceVersion = sourceVersion;
        }

        public ReplicationReadSuccess Updated(ImmutableArray<DurableEvent> events, VectorTime version) =>
            new ReplicationReadSuccess(
                correlationId: CorrelationId,
                events: events,
                @from: From,
                progress: Progress,
                targetId: TargetId,
                sourceVersion: version);

        public bool Equals(ReplicationReadSuccess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId
                && Events.SequenceEqual(other.Events)
                && From == other.From
                && Progress == other.Progress
                && string.Equals(TargetId, other.TargetId)
                && SourceVersion.Equals(other.SourceVersion);
        }

        public override bool Equals(object obj) => obj is ReplicationReadSuccess && Equals((ReplicationReadSuccess)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CorrelationId.GetHashCode();
                hashCode = (hashCode * 397) ^ Events.GetHashCode();
                hashCode = (hashCode * 397) ^ From.GetHashCode();
                hashCode = (hashCode * 397) ^ Progress.GetHashCode();
                hashCode = (hashCode * 397) ^ (TargetId != null ? TargetId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SourceVersion.GetHashCode();
                return hashCode;
            }
        }
    }

    public sealed class ReplicationReadFailure : ReplicationReadReply, IEquatable<ReplicationReadFailure>
    {
        public ReplicationReadException Cause { get; }

        public override bool IsSuccess => false;
        public override bool IsFailure => true;

        public ReplicationReadFailure(int correlationId, ReplicationReadException cause, string targetId)
            : base(targetId, correlationId)
        {
            Cause = cause;
        }

        public bool Equals(ReplicationReadFailure other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Cause, other.Cause) && string.Equals(TargetId, other.TargetId);
        }

        public override bool Equals(object obj) => obj is ReplicationReadFailure && Equals((ReplicationReadFailure)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Cause != null ? Cause.GetHashCode() : 0) * 397) ^ (TargetId != null ? TargetId.GetHashCode() : 0) ^ CorrelationId;
            }
        }

        public override string ToString() => $"ReplicationReadFailure(target: {TargetId}, correlationId: {CorrelationId}, cause: {Cause})";
    }

    public abstract class ReplicationReadException : Exception
    {
        protected ReplicationReadException(string message) : base(message) { }
        protected ReplicationReadException(string message, Exception innerException) : base(message, innerException) { }
        protected ReplicationReadException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class ReplicationReadSourceException : ReplicationReadException
    {
        public ReplicationReadSourceException(string message) : base(message) { }
        public ReplicationReadSourceException(string message, Exception innerException) : base(message, innerException) { }
        public ReplicationReadSourceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class ReplicationReadTimeoutException : ReplicationReadException
    {
        public TimeSpan Timeout { get; }

        public ReplicationReadTimeoutException(TimeSpan timeout) : base($"Replication read timed out after {timeout}")
        {
            Timeout = timeout;
        }


        public ReplicationReadTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            var ticks = info.GetInt64("timeout");
            Timeout = new TimeSpan(ticks);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("timeout", Timeout);
        }
    }

    #endregion

    #region Replication write

    public sealed class ReplicationWriteBatch : IReplicationRequest, IEquatable<ReplicationWriteBatch>
    {
        public ReplicationWriteBatch(ImmutableArray<ReplicationWrite> batch)
        {
            Batch = batch;
        }

        public ImmutableArray<ReplicationWrite> Batch { get; }

        public bool Equals(ReplicationWriteBatch other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Batch.SequenceEqual(other.Batch);
        }

        public override bool Equals(object obj) => obj is ReplicationWriteBatch && Equals((ReplicationWriteBatch)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 0;
                foreach (var write in Batch)
                {
                    hash ^= (hash * 397) ^ write.GetHashCode();
                }
                return hash;
            }
        }

        public override string ToString() => $"ReplicationWriteBatch({string.Join(", ", Batch)})";
    }

    public sealed class ReplicationWriteBatchComplete
    {
        public static readonly ReplicationWriteBatchComplete Instance = new ReplicationWriteBatchComplete();
        private ReplicationWriteBatchComplete() { }
    }

    public struct ReplicationMetadata : IEquatable<ReplicationMetadata>
    {
        public long Progress { get; }
        public VectorTime Version { get; }

        public ReplicationMetadata(long progress, VectorTime version)
        {
            Progress = progress;
            Version = version;
        }

        public ReplicationMetadata WithVersion(VectorTime version) =>
            new ReplicationMetadata(this.Progress, version);

        public bool Equals(ReplicationMetadata other) =>
            Progress == other.Progress && Version.Equals(other.Version);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ReplicationMetadata && Equals((ReplicationMetadata)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Progress.GetHashCode() * 397) ^ Version.GetHashCode();
            }
        }

        public override string ToString() => $"ReplicationMetadata(progress: {Progress}, version: {Version})";
    }

    public sealed class ReplicationWrite : IReplicationRequest, IReplyable<ReplicationWriteReply>, IEquatable<ReplicationWrite>
    {
        public ImmutableArray<DurableEvent> Events { get; }
        public ImmutableDictionary<string, ReplicationMetadata> Metadata { get; }
        public bool ContinueReplication { get; }
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public ReplicationWrite(ImmutableArray<DurableEvent> events, ImmutableDictionary<string, ReplicationMetadata> metadata, int correlationId, bool continueReplication = false, IActorRef replyTo = null)
        {
            Events = events;
            Metadata = metadata;
            ContinueReplication = continueReplication;
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public ReplicationWrite Update(ImmutableArray<DurableEvent> events) =>
            new ReplicationWrite(
                events: events,
                metadata: this.Metadata,
                correlationId: this.CorrelationId,
                continueReplication: this.ContinueReplication,
                replyTo: this.ReplyTo);

        public ImmutableDictionary<string, long> Progresses => Metadata
            .Select(kv => new KeyValuePair<string, long>(kv.Key, kv.Value.Progress))
            .ToImmutableDictionary();

        public IEnumerable<string> SourceIds => Metadata.Keys;

        public bool Equals(ReplicationWrite other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId
                && Events.SequenceEqual(other.Events)
                && Metadata.DictionaryEquals(other.Metadata)
                && ContinueReplication == other.ContinueReplication
                && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj) => obj is ReplicationWrite && Equals((ReplicationWrite)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Events.GetHashCode();
                hashCode = (hashCode * 397) ^ (Metadata?.DictionaryHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ ContinueReplication.GetHashCode();
                hashCode = (hashCode * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (ReplyTo?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    public abstract class ReplicationWriteReply : IReplicationReply
    {
        protected ReplicationWriteReply(int correlationId)
        {
            CorrelationId = correlationId;
        }

        public int CorrelationId { get; }
        public abstract bool IsSuccess { get; }
        public abstract bool IsFailure { get; }
    }

    public sealed class ReplicationWriteSuccess : ReplicationWriteReply, IEquatable<ReplicationWriteSuccess>
    {
        public ImmutableArray<DurableEvent> Events { get; }
        public ImmutableDictionary<string, ReplicationMetadata> Metadata { get; }
        public bool ContinueReplication { get; }

        public override bool IsSuccess => true;
        public override bool IsFailure => false;

        public ReplicationWriteSuccess(int correlationId, ImmutableArray<DurableEvent> events, ImmutableDictionary<string, ReplicationMetadata> metadata, bool continueReplication) : base(correlationId)
        {
            Events = events;
            Metadata = metadata;
            ContinueReplication = continueReplication;
        }

        public bool Equals(ReplicationWriteSuccess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId
                && Events.SequenceEqual(other.Events)
                && Metadata.DictionaryEquals(other.Metadata)
                && ContinueReplication == other.ContinueReplication;
        }

        public override bool Equals(object obj) => obj is ReplicationWriteSuccess && Equals((ReplicationWriteSuccess)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Events.GetHashCode() * 397) ^ CorrelationId;
                hashCode = (hashCode * 397) ^ (Metadata?.DictionaryHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ ContinueReplication.GetHashCode();
                return hashCode;
            }
        }
    }

    public sealed class ReplicationWriteFailure : ReplicationWriteReply, IEquatable<ReplicationWriteFailure>
    {
        public Exception Cause { get; }

        public override bool IsSuccess => false;
        public override bool IsFailure => true;

        public ReplicationWriteFailure(int correlationId, Exception cause) : base(correlationId)
        {
            Cause = cause;
        }

        public bool Equals(ReplicationWriteFailure other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(Cause, other.Cause);
        }

        public override bool Equals(object obj) => obj is ReplicationWriteFailure && Equals((ReplicationWriteFailure)obj);

        public override int GetHashCode()
        {
            return (CorrelationId * 397) ^ Cause.GetHashCode();
        }
    }

    #endregion

    #region Clock adjustment

    public sealed class AdjustClock : IReplicationRequest, IReplyable<AdjustClockReply>, IEquatable<AdjustClock>
    {
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public AdjustClock(int correlationId, IActorRef replyTo)
        {
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(AdjustClock other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj) => obj is AdjustClock && Equals((AdjustClock)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (CorrelationId * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
            }
        }

        public override string ToString() => $"AdjustClock(correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public abstract class AdjustClockReply : IReplicationReply
    {
        protected AdjustClockReply(int correlationId)
        {
            CorrelationId = correlationId;
        }

        public int CorrelationId { get; }
    }

    public sealed class AdjustClockSuccess : AdjustClockReply, IEquatable<AdjustClockSuccess>
    {
        public Version Clock { get; }

        public AdjustClockSuccess(Version clock, int correlationId) : base(correlationId)
        {
            Clock = clock;
        }

        public bool Equals(AdjustClockSuccess other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Clock.Equals(other.Clock);
        }

        public override bool Equals(object obj) => obj is AdjustClockSuccess && Equals((AdjustClockSuccess)obj);

        public override int GetHashCode()
        {
            return (CorrelationId) ^ Clock.GetHashCode();
        }

        public override string ToString() => $"AdjustClockSuccess({Clock}, correlationId: {CorrelationId})";
    }

    public sealed class AdjustClockFailure : AdjustClockReply, IEquatable<AdjustClockFailure>
    {
        public Exception Cause { get; }

        public AdjustClockFailure(Exception cause, int correlationId) : base(correlationId)
        {
            Cause = cause;
        }

        public bool Equals(AdjustClockFailure other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Cause.Equals(other.Cause);
        }

        public override bool Equals(object obj) => obj is AdjustClockFailure && Equals((AdjustClockFailure)obj);

        public override int GetHashCode()
        {
            return (CorrelationId) ^ Cause.GetHashCode();
        }

        public override string ToString() => $"AdjustClockFailure({Cause}, correlationId: {CorrelationId})";
    }

    public sealed class GetVersion : IReplicationRequest, IReplyable<GetVersionReply>, IEquatable<GetVersion>
    {
        public int CorrelationId { get; }
        public IActorRef ReplyTo { get; }

        public GetVersion(int correlationId, IActorRef replyTo)
        {
            CorrelationId = correlationId;
            ReplyTo = replyTo;
        }

        public bool Equals(GetVersion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CorrelationId == other.CorrelationId && Equals(ReplyTo, other.ReplyTo);
        }

        public override bool Equals(object obj) => obj is GetVersion && Equals((GetVersion)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (CorrelationId * 397) ^ (ReplyTo != null ? ReplyTo.GetHashCode() : 0);
            }
        }

        public override string ToString() => $"GetVersion(correlationId: {CorrelationId}, replyTo: {ReplyTo})";
    }

    public class GetVersionReply : IReplicationReply, IEquatable<GetVersionReply>
    {
        public Version Clock { get; }

        public int CorrelationId { get; }

        public GetVersionReply(Version clock, int correlationId)
        {
            Clock = clock;
            CorrelationId = correlationId;
        }

        public bool Equals(GetVersionReply other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Clock, other.Clock) && CorrelationId == other.CorrelationId;
        }

        public override bool Equals(object obj) => obj is GetVersionReply && Equals((GetVersionReply)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Clock.GetHashCode() * 397) ^ CorrelationId;
            }
        }

        public override string ToString() => $"GetVersionReply({Clock}, correlationId: {CorrelationId})";
    }

    #endregion
}