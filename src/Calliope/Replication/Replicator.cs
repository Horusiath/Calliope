#region copyright
// -----------------------------------------------------------------------
//  <copyright file="Replicator.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace Calliope.Replication
{
    using ReplicaId = Address;

    public static class Replicator
    {
        public sealed class Sent<T> : IEquatable<Sent<T>>
        {
            public ReplicaId Target { get; }
            public Versioned<T> Versioned { get; }

            public Sent(ReplicaId target, Versioned<T> versioned)
            {
                Target = target;
                Versioned = versioned;
            }

            public bool Equals(Sent<T> other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Target, other.Target) && Versioned.Equals(other.Versioned);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Sent<T> && Equals((Sent<T>) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Target != null ? Target.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Versioned.GetHashCode();
                    return hashCode;
                }
            }

            public override string ToString() => $"Sent({Target}, {Versioned})";
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
                return obj is PendingAck<T> && Equals((PendingAck<T>) obj);
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
            public Versioned<T> Versioned { get; }
            public ImmutableHashSet<ReplicaId> SeenBy { get; }

            public Send(Versioned<T> versioned, ImmutableHashSet<ReplicaId> seenBy)
            {
                Versioned = versioned;
                SeenBy = seenBy;
            }

            public Send<T> UpdateSeenBy(ReplicaId address) => new Send<T>(Versioned, SeenBy.Add(address));

            public bool Equals(Send<T> other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Versioned.Equals(other.Versioned)
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
                    var hashCode = Versioned.GetHashCode();
                    foreach (var address in SeenBy)
                    {
                        hashCode = (hashCode * 397) ^ address.GetHashCode();
                    }
                    return hashCode;
                }
            }

            public override string ToString() => $"Send({Versioned}, seenBy:{string.Join(", ", SeenBy)})";
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
    
    internal sealed class Replicator<T> : ReceiveActor
    {
        private readonly ILoggingAdapter log = Context.GetLogger();
        private readonly Cluster cluster = Cluster.Get(Context.System);

        // settings
        private readonly TimeSpan checkRetryInterval = TimeSpan.FromSeconds(10);
        private readonly TimeSpan retryTimeout = TimeSpan.FromSeconds(10);
        private readonly string role = null;
        private readonly ICancelable resendTask;

        // state

        private VClock localVersion = VClock.Zero;
        private VClock latestStableVersion = VClock.Zero;
        private ImmutableDictionary<ReplicaId, VClock> remoteVersions = ImmutableDictionary<ReplicaId, VClock>.Empty;
        private ImmutableHashSet<ReplicaId> members = ImmutableHashSet<ReplicaId>.Empty;
        private ImmutableHashSet<IActorRef> subscribers = ImmutableHashSet<IActorRef>.Empty;

        private ImmutableHashSet<Replicator.Sent<T>> pendingDelivery = ImmutableHashSet<Replicator.Sent<T>>.Empty;
        private ImmutableHashSet<Replicator.PendingAck<T>> pendingAcks = ImmutableHashSet<Replicator.PendingAck<T>>.Empty;
        
        public Replicator()
        {
            Receive<ClusterEvent.MemberUp>(up =>
            {
                if (HasRole(up.Member) && up.Member.Address != cluster.SelfAddress)
                    members = members.Add(up.Member.Address);
            });
            Receive<ClusterEvent.MemberRemoved>(removed => members = members.Remove(removed.Member.Address));
            Receive<ClusterEvent.IMemberEvent>(_ => { /* ignore */ });
            Receive<Replicator.Broadcast<T>>(bcast =>
            {
                var version = localVersion.Increment(cluster.SelfAddress.ToString());
                var versioned = new Versioned<T>(version, bcast.Message);
                var send = new Replicator.Send<T>(versioned, ImmutableHashSet.Create(Self.Path.Address));

                if (log.IsInfoEnabled) log.Info("Sending {0} to: {1}", send, string.Join(", ", members));

                foreach (var address in members)
                    Send(send, address);

                var pendingAck = new Replicator.PendingAck<T>(cluster.SelfAddress, versioned, DateTime.UtcNow, members);

                this.pendingAcks = pendingAcks.Add(pendingAck);
                this.localVersion = version;
            });
            Receive<Replicator.Send<T>>(send =>
            {
                if (AlreadySeen(send.Versioned.Version))
                {
                    log.Info("Received duplicate message {0} from {1}", send, Sender);
                }
                else
                {
                    var receivers = members.Remove(Sender.Path.Address);

                    var forward = send.UpdateSeenBy(Self.Path.Address);
                    if (log.IsInfoEnabled) log.Info("Broadcasting message {0} to: {1}", forward, string.Join(", ", receivers));

                    foreach (var address in receivers)
                        Send(send, address);

                    var ack = new Replicator.SendAck(send.Versioned.Version);
                    Sender.Tell(ack);

                    Self.Forward(new Replicator.Deliver<T>(send.Versioned));

                    var senderAddr = Sender.Path.Address;

                    this.pendingDelivery = pendingDelivery.Add(new Replicator.Sent<T>(senderAddr, send.Versioned));
                    this.pendingAcks = pendingAcks.Add(new Replicator.PendingAck<T>(senderAddr, send.Versioned, DateTime.UtcNow, receivers));
                }
            });
            Receive<Replicator.SendAck>(ack =>
            {
                log.Info("Received ACK from {0} (version: {1})", Sender, ack.Version);

                var pendingAck = this.pendingAcks.First(x => x.Versioned.Version == ack.Version);
                var membersLeft = pendingAck.Members.Remove(Sender.Path.Address);

                this.pendingAcks = pendingAcks.Remove(pendingAck);
                if (!membersLeft.IsEmpty) this.pendingAcks = pendingAcks.Add(pendingAck.WithMembers(membersLeft));
            });
            Receive<Replicator.Deliver<T>>(deliver =>
            {
                var t = CasuallyDeliver(); //TODO: implement
                localVersion = t.Item1;
                pendingDelivery = t.Item2;

                remoteVersions = remoteVersions.SetItem(Sender.Path.Address, deliver.Versioned.Version);
                latestStableVersion = UpdateStableVersion();
            });
            Receive<Replicator.Resend>(_ =>
            {
                var now = DateTime.UtcNow;
                var builder = pendingAcks.ToBuilder();
                foreach (var ack in pendingAcks)
                {
                    if (now - ack.Timestamp > retryTimeout)
                    {
                        builder.Remove(ack);
                        var send = new Replicator.Send<T>(ack.Versioned, ImmutableHashSet.Create(Self.Path.Address));
                        foreach (var member in ack.Members)
                        {
                            Send(send, member);
                        }
                        builder.Add(ack.WithTimestamp(now));
                    }
                }
                pendingAcks = builder.ToImmutable();
            });
            Receive<Replicator.StableReq>(sync =>
            {
                var reply = sync.Versions.Where(ver => latestStableVersion >= ver).ToArray();
                Sender.Tell(new Replicator.StableRep(reply));
            });
            Receive<Replicator.Subscribe>(subscribe =>
            {
                subscribers = subscribers.Add(subscribe.Ref);
                if (subscribe.Ack != null) subscribe.Ref.Tell(subscribe.Ack);
            });
            Receive<Replicator.Unsubscribe>(unsubscribe =>
            {
                subscribers = subscribers.Remove(unsubscribe.Ref);
                if (unsubscribe.Ack != null) unsubscribe.Ref.Tell(unsubscribe.Ack);
            });
            Receive<Terminated>(terminated => subscribers = subscribers.Remove(terminated.ActorRef));

            resendTask = Context.System.Scheduler
                .ScheduleTellOnceCancelable(checkRetryInterval, Self, Replicator.Resend.Instance, ActorRefs.NoSender);
        }

        private VClock UpdateStableVersion()
        {
            throw new NotImplementedException();
        }

        private bool AlreadySeen(VClock version) => localVersion >= version || pendingDelivery.Any(x => x.Versioned.Version == version);

        private void Send(Replicator.Send<T> send, ReplicaId member)
        {
            var targetPath = Self.Path.ToStringWithAddress(member);
            Context.ActorSelection(targetPath).Tell(send, Sender);
        }

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

        private bool HasRole(Member member) => string.IsNullOrEmpty(role) || member.Roles.Contains(role);

        #region tagged reliable casual broadcast


        /// <summary>
        /// Check and possibly deliver a message if it should be delivered.
        /// </summary>
        /// <returns></returns>
        private Tuple<VClock, ImmutableHashSet<Replicator.Sent<T>>> CasuallyDeliver()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}