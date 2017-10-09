#region copyright
// -----------------------------------------------------------------------
//  <copyright file="cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Util;

namespace Calliope.Replication
{
    using ReplicaId = Int32;

    internal sealed class Replicator<T> : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();
        private readonly Cluster cluster = Cluster.Get(Context.System);
        private readonly ICancelable resendTask;

        // settings
        private readonly ReplicatorSettings settings;
        private readonly string replicatorRelativePath;
        private readonly ReplicaId myself;

        // state
        private VClock localVersion = VClock.Zero;
        private VClock latestStableVersion = VClock.Zero;

        // TODO: replace with matrix clock
        private ImmutableDictionary<ReplicaId, VClock> remoteVersions = ImmutableDictionary<ReplicaId, VClock>.Empty;
        private ImmutableDictionary<ReplicaId, IActorRef> members = ImmutableDictionary<ReplicaId, IActorRef>.Empty;
        private ImmutableHashSet<IActorRef> subscribers = ImmutableHashSet<IActorRef>.Empty;

        private ImmutableHashSet<Deliver<T>> pendingDelivery = ImmutableHashSet<Deliver<T>>.Empty;
        private ImmutableHashSet<PendingAck<T>> pendingAcks = ImmutableHashSet<PendingAck<T>>.Empty;

        public Replicator(string replicaId, ReplicatorSettings settings) 
            : this(MurmurHash.StringHash(replicaId), settings) { }

        public Replicator(ReplicaId myself, ReplicatorSettings settings)
        {
            this.myself = myself;
            this.settings = settings;
            replicatorRelativePath = Self.Path.ToStringWithoutAddress();

            Receive<ClusterEvent.MemberUp>(up =>
            {
                if (HasRole(up.Member) && up.Member.Address != cluster.SelfAddress)
                {
                    // send invitation to a new member
                    var path = up.Member.Address.ToString() + replicatorRelativePath;
                    Context.ActorSelection(path).Tell(new Invitation(myself, Self));
                }
            });
            Receive<ClusterEvent.IMemberEvent>(_ => { /* ignore */ });
            Receive<Broadcast<T>>(bcast =>
            {
                var version = localVersion.Increment(myself);
                var versioned = new Versioned<T>(version, bcast.Message);
                var send = new Send<T>(myself, myself, versioned);

                if (log.IsInfoEnabled) log.Info("Sending {0} to: {1}", send, string.Join(", ", members));

                foreach (var member in members)
                    member.Value.Forward(send);

                var pendingAck = new PendingAck<T>(myself, versioned, DateTime.UtcNow, members.Keys.ToImmutableHashSet());

                this.pendingAcks = pendingAcks.Add(pendingAck);
                this.localVersion = version;
            });
            Receive<Send<T>>(send =>
            {
                if (AlreadySeen(send.Versioned.Version))
                {
                    log.Info("Received duplicate message {0}", send);
                }
                else
                {
                    var receivers = members.Remove(send.Origin);

                    var forward = send.WithLastSeenBy(myself);
                    if (log.IsInfoEnabled) log.Info("Broadcasting message {0} to: {1}", forward, string.Join(", ", receivers.Values));

                    foreach (var member in receivers.Values)
                        member.Forward(forward);
                    
                    Sender.Tell(new SendAck(myself, send.Versioned.Version));

                    var deliver = new Deliver<T>(send.Origin, send.Versioned);
                    Self.Forward(deliver);

                    this.pendingDelivery = pendingDelivery.Add(deliver);
                    this.pendingAcks = pendingAcks.Add(new PendingAck<T>(myself, send.Versioned, DateTime.UtcNow, receivers.Keys.ToImmutableHashSet()));
                }
            });
            Receive<SendAck>(ack =>
            {
                log.Info("Received ACK from {0} (version: {1})", Sender, ack.Version);

                var pendingAck = this.pendingAcks.First(x => x.Versioned.Version == ack.Version);
                var membersLeft = pendingAck.Members.Remove(ack.ReplicaId);

                this.pendingAcks = pendingAcks.Remove(pendingAck);
                if (!membersLeft.IsEmpty) this.pendingAcks = pendingAcks.Add(pendingAck.WithMembers(membersLeft));
            });
            Receive<Deliver<T>>(deliver =>
            {
                TryToCasuallyDeliver(deliver); 

                remoteVersions = remoteVersions.SetItem(deliver.Origin, deliver.Versioned.Version);
                latestStableVersion = UpdateStableVersion(remoteVersions);
            });
            Receive<Resend>(_ =>
            {
                var now = DateTime.UtcNow;
                var builder = pendingAcks.ToBuilder();
                foreach (var ack in pendingAcks)
                {
                    if (now - ack.Timestamp > settings.RetryTimeout)
                    {
                        builder.Remove(ack);
                        var send = new Send<T>(myself, myself, ack.Versioned);
                        foreach (var replicaId in ack.Members)
                        {
                            if (members.TryGetValue(replicaId, out var member))
                                member.Tell(send);
                        }
                        builder.Add(ack.WithTimestamp(now));
                    }
                }
                pendingAcks = builder.ToImmutable();
            });
            Receive<Invitation>(invitation =>
            {
                members = members.Add(invitation.ReplicaId, invitation.ReplicatorRef);
                Context.Watch(invitation.ReplicatorRef);
            });
            Receive<StableReq>(sync =>
            {
                var reply = sync.Versions.Where(ver => latestStableVersion >= ver).ToArray();
                Sender.Tell(new StableRep(reply));
            });
            Receive<Subscribe<T>>(subscribe =>
            {
                subscribers = subscribers.Add(subscribe.Ref);
                if (subscribe.Ack != null) subscribe.Ref.Tell(subscribe.Ack);
            });
            Receive<Unsubscribe>(unsubscribe =>
            {
                subscribers = subscribers.Remove(unsubscribe.Ref);
                if (unsubscribe.Ack != null) unsubscribe.Ref.Tell(unsubscribe.Ack);
            });
            Receive<Terminated>(terminated =>
            {
                subscribers = subscribers.Remove(terminated.ActorRef);
                var replicaId = members.FirstOrDefault(kv => Equals(kv.Value, terminated.ActorRef));
                members = members.Remove(replicaId.Key);
            });

            resendTask = Context.System.Scheduler
                .ScheduleTellOnceCancelable(settings.ResendInterval, Self, Resend.Instance, ActorRefs.NoSender);
        }

        /// <summary>
        /// Returns latests stable version which is an aggregate of the least values of all known remote versions times for individual replica id.
        /// </summary>
        private static VClock UpdateStableVersion(ImmutableDictionary<ReplicaId, VClock> versions)
        {
            var first = versions.First().Value;
            return versions.Values.Aggregate(first, (c1, c2) => c1.MergeMin(c2));
        }

        private bool AlreadySeen(VClock version) => localVersion >= version || pendingDelivery.Any(x => x.Versioned.Version == version);

        protected override void PreStart()
        {
            cluster.Subscribe(Self, ClusterEvent.SubscriptionInitialStateMode.InitialStateAsEvents, typeof(ClusterEvent.IMemberEvent));
            base.PreStart();
        }

        protected override void PostStop()
        {
            cluster.Unsubscribe(Self);
            resendTask.Cancel();
            base.PostStop();
        }

        private bool HasRole(Member member) => string.IsNullOrEmpty(settings.Role) || member.Roles.Contains(settings.Role);

        #region tagged reliable casual broadcast

        /// <summary>
        /// Check and possibly deliver a message if it should be delivered.
        /// </summary>
        /// <returns></returns>
        private bool TryToCasuallyDeliver(Deliver<T> delivery)
        {
            if (ShouldBeDelivered(delivery.Origin, localVersion, delivery.Versioned.Version))
            {
                foreach (var subscriber in subscribers)
                {
                    subscriber.Tell(delivery.Versioned);
                }

                pendingDelivery = pendingDelivery.Remove(delivery);
                localVersion = localVersion.Increment(delivery.Origin);

                return true;
            }
            return false;
        }

        //TODO: move it into easily testable class
        public static bool ShouldBeDelivered(ReplicaId origin, VClock local, VClock remote)
        {
            var lver = local.Value;
            foreach ((var rkey, var rtime) in remote.Value)
            {
                if (lver.TryGetValue(rkey, out var ltime))
                {
                    if (rkey == origin)
                    {
                        if (rtime != ltime + 1) return false;
                    }
                    else
                    {
                        if (rtime > ltime) return false;
                    }
                }
                else if (!(rkey == origin && rtime == 1))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}