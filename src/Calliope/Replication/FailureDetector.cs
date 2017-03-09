using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;

namespace Calliope.Replication
{
    internal sealed class FailureDetector : ReceiveActor
    {
        #region messages

        public sealed class AvailabilityDetected
        {
            public static readonly AvailabilityDetected Instance = new AvailabilityDetected();
            private AvailabilityDetected() { }
        }

        public sealed class FailureDetected
        {
            public FailureDetected(Exception cause)
            {
                Cause = cause;
            }

            public Exception Cause { get; }
        }

        public sealed class FailureDetectionLimitReached
        {
            public FailureDetectionLimitReached(int count)
            {
                Count = count;
            }

            public int Count { get; }
        }

        #endregion

        private int count = 0;
        private ILoggingAdapter log;
        public ILoggingAdapter Log => log ?? (log = Context.GetLogger());

        public FailureDetector(string sourceEndpointId, string journalId, TimeSpan detectionLimit)
        {
            var causes = new List<Exception>();
            var lastReportedAvailability = DateTime.MinValue;
            var schedule = ScheduleFailureDetectionLimitReached(detectionLimit);

            Receive<AvailabilityDetected>(_ =>
            {
                var now = DateTime.UtcNow;
                var lastInterval = now - lastReportedAvailability;
                if (lastInterval >= detectionLimit)
                {
                    Context.System.EventStream.Publish(new Available(sourceEndpointId, journalId));
                    lastReportedAvailability = now;
                }

                // reschedule
                schedule.Cancel();
                schedule = ScheduleFailureDetectionLimitReached(detectionLimit);
                causes.Clear();
            });
            Receive<FailureDetected>(detected => causes.Add(detected.Cause));
            Receive<FailureDetectionLimitReached>(reached => reached.Count == count, reached =>
            {
                var cause = new AggregateException(causes.ToArray());
                var unavailable = new Unavailable(sourceEndpointId, journalId, cause);
                Log.Error(cause, 
                    "Replication failure detection limit reached {0}. Publishing {1}.",
                    detectionLimit, unavailable);
                Context.System.EventStream.Publish(unavailable);
                ScheduleFailureDetectionLimitReached(detectionLimit);
                causes.Clear();
            });
        }

        private ICancelable ScheduleFailureDetectionLimitReached(TimeSpan detectionLimit) =>
            Context.System.Scheduler.ScheduleTellOnceCancelable(detectionLimit, Self, new FailureDetectionLimitReached(++count), Self);
    }
    /// <summary>
    /// Published via system event stream once remote journal is available
    /// </summary>
    public sealed class Available : IEquatable<Available>
    {
        public string EndpointId { get; }
        public string JournalId { get; }

        public Available(string endpointId, string journalId)
        {
            EndpointId = endpointId;
            JournalId = journalId;
        }

        public bool Equals(Available other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(EndpointId, other.EndpointId) && string.Equals(JournalId, other.JournalId);
        }

        public override bool Equals(object obj) => obj is Available && Equals((Available)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (EndpointId.GetHashCode() * 397) ^ JournalId.GetHashCode();
            }
        }

        public override string ToString() => $"Available({EndpointId}, {JournalId})";
    }
    /// <summary>
    /// Published via system event stream once remote journal is unavailable
    /// </summary>
    public sealed class Unavailable : IEquatable<Unavailable>
    {
        public string EndpointId { get; }
        public string JournalId { get; }
        public AggregateException Cause { get; }

        public Unavailable(string endpointId, string journalId, AggregateException cause)
        {
            EndpointId = endpointId;
            JournalId = journalId;
            Cause = cause;
        }

        public bool Equals(Unavailable other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(EndpointId, other.EndpointId) 
                && string.Equals(JournalId, other.JournalId)
                && Equals(Cause, other.Cause);
        }

        public override bool Equals(object obj) => obj is Unavailable && Equals((Unavailable)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EndpointId.GetHashCode();
                hashCode = (hashCode * 397) ^ (JournalId.GetHashCode());
                hashCode = (hashCode * 397) ^ (Cause.GetHashCode());
                return hashCode;
            }
        }

        public override string ToString() => $"Unavailable({EndpointId}, {JournalId})";
    }
}