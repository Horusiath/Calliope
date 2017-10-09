#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ReplicatorState.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Calliope.Replication
{
    using ReplicaId = Int32;
    // TODO: replace with a proper matrix clock
    using MClock = ImmutableDictionary<Int32, VClock>;

    public class ReplicatorState<T>
    {
        public static ReplicatorState<T> Empty = new ReplicatorState<T>(
            localVersion: VClock.Zero, 
            stableVersion: VClock.Zero, 
            remoteVersions: MClock.Empty,
            pendingDeliveries: ImmutableHashSet<Replicator.Deliver<T>>.Empty, 
            pendingAcks: ImmutableHashSet<Replicator.PendingAck<T>>.Empty);

        public VClock LocalVersion { get; }
        public VClock StableVersion { get; }
        public MClock RemoteVersions { get; }
        public ImmutableHashSet<Replicator.Deliver<T>> PendingDeliveries { get; }
        public ImmutableHashSet<Replicator.PendingAck<T>> PendingAcks { get; }

        public ReplicatorState(
            VClock localVersion,
            VClock stableVersion,
            ImmutableDictionary<ReplicaId, VClock> remoteVersions,
            ImmutableHashSet<Replicator.Deliver<T>> pendingDeliveries,
            ImmutableHashSet<Replicator.PendingAck<T>> pendingAcks)
        {
            LocalVersion = localVersion;
            StableVersion = stableVersion;
            RemoteVersions = remoteVersions;
            PendingDeliveries = pendingDeliveries;
            PendingAcks = pendingAcks;
        }

        public ReplicatorState<T> WithRemoteVersions(MClock clock) =>
            new ReplicatorState<T>(
                localVersion: LocalVersion,
                stableVersion: UpdateStableVersion(clock),
                remoteVersions: clock,
                pendingDeliveries: PendingDeliveries,
                pendingAcks: PendingAcks);

        public ReplicatorState<T> WithLocalVersion(VClock localVersion) =>
            new ReplicatorState<T>(
                localVersion: localVersion,
                stableVersion: StableVersion,
                remoteVersions: RemoteVersions,
                pendingDeliveries: PendingDeliveries,
                pendingAcks: PendingAcks);

        public ReplicatorState<T> WithPendingAcks(ImmutableHashSet<Replicator.PendingAck<T>> acks) =>
            new ReplicatorState<T>(
                localVersion: LocalVersion,
                stableVersion: StableVersion,
                remoteVersions: RemoteVersions,
                pendingDeliveries: PendingDeliveries,
                pendingAcks: acks);

        public ReplicatorState<T> WithPendingDeliveries(ImmutableHashSet<Replicator.Deliver<T>> deliveries) =>
            new ReplicatorState<T>(
                localVersion: LocalVersion,
                stableVersion: StableVersion,
                remoteVersions: RemoteVersions,
                pendingDeliveries: deliveries,
                pendingAcks: PendingAcks);


        /// <summary>
        /// Returns latests stable version which is an aggregate of the least values of all known remote versions times for individual replica id.
        /// </summary>
        private static VClock UpdateStableVersion(ImmutableDictionary<ReplicaId, VClock> versions)
        {
            var first = versions.First().Value;
            return versions.Values.Aggregate(first, (c1, c2) => c1.MergeMin(c2));
        }
    }
}