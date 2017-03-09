using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Calliope.Persistence;
using Calliope.Versioning;

namespace Calliope.Replication.Notifications
{
    /// <summary>
    /// Notifies registered replicators about its parent event journal updates.
    /// </summary>
    internal sealed class NotificationChannel : ReceiveActor
    {
        #region internal classes

        public struct Updated
        {
            public readonly ImmutableArray<DurableEvent> Events;

            public Updated(ImmutableArray<DurableEvent> events)
            {
                Events = events;
            }
        }

        internal struct Registration
        {
            public readonly IActorRef ReplicatorRef;
            public readonly DateTime RegistrationTime;
            public readonly VectorTime CurrentVersion;
            public readonly ReplicationFilter Filter;

            public Registration(ReplicationRead read)
                : this(read.Replicator, DateTime.UtcNow, read.TargetVersion, read.Filter)
            {
            }

            public Registration(IActorRef replicatorRef, DateTime registrationTime, VectorTime currentVersion, ReplicationFilter filter)
            {
                ReplicatorRef = replicatorRef;
                RegistrationTime = registrationTime;
                CurrentVersion = currentVersion;
                Filter = filter;
            }

            public Registration WithCurrentVersion(VectorTime version) =>
                new Registration(ReplicatorRef, RegistrationTime, version, Filter);
        }

        #endregion

        public static Akka.Actor.Props Props(TimeSpan registrationExpiration) =>
            Akka.Actor.Props.Create(() => new NotificationChannel(registrationExpiration)).WithDeploy(Deploy.Local);

        public NotificationChannel(TimeSpan registrationExpiration)
        {
            var registry = new Dictionary<string, Registration>();
            var reading = new HashSet<string>();
            Receive<Updated>(updated =>
            {
                var date = DateTime.UtcNow;
                foreach (var entry in registry)
                {
                    var reg = entry.Value;
                    if (!reading.Contains(entry.Key)
                        && updated.Events.Any(e => e.CanReplicate(reg.CurrentVersion, reg.Filter))
                        && date - reg.RegistrationTime <= registrationExpiration)
                    {
                        reg.ReplicatorRef.Tell(ReplicationDue.Instance);
                    }
                }
            });
            Receive<ReplicationRead>(read =>
            {
                registry[read.TargetId] = new Registration(read);
                reading.Add(read.TargetId);
            });
            Receive<ReplicationReadReply>(success =>
            {
                reading.Remove(success.TargetId);
            });
            Receive<ReplicationWrite>(write =>
            {
                foreach (var sourceId in write.SourceIds)
                {
                    Registration registration;
                    if (registry.TryGetValue(sourceId, out registration))
                    {
                        var meta = write.Metadata[sourceId];
                        registry[sourceId] = registration.WithCurrentVersion(meta.Version);
                    }
                }
            });
        }
    }
}