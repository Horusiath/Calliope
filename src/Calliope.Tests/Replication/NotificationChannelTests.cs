using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Calliope.Persistence;
using Calliope.Replication;
using Calliope.Replication.Notifications;
using Calliope.Versioning;
using Xunit;
using Xunit.Abstractions;
using Version = System.Version;

namespace Calliope.Tests.Replication
{
    public class NotificationChannelTests : TestKit
    {
        private const string SourceId = "sid";
        private const string TargetId1 = "tid1";
        private const string TargetId2 = "tid2";

        private readonly IActorRef channel;
        private readonly TestProbe probe;

        private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(200);

        public NotificationChannelTests(ITestOutputHelper output) : base(output: output)
        {
            channel = Sys.ActorOf(NotificationChannel.Props(TimeSpan.FromMilliseconds(400)), "notification-channel");
            probe = CreateTestProbe();
        }

        [Fact]
        public void Notification_channel_should_send_notification_if_an_update_is_not_in_casual_past_of_the_target()
        {
            SourceRead(TargetId1, VTime(0, 1, 0));
            SourceUpdate(Event("a", VTime(1, 0, 0)));

            probe.ExpectMsg(ReplicationDue.Instance);
            probe.ExpectNoMsg(Timeout);
        }

        [Fact]
        public void Notification_channel_should_send_notification_if_part_of_an_update_is_not_in_casual_past_of_the_target()
        {
            SourceRead(TargetId1, VTime(0, 1, 0));
            SourceUpdate(Event("a", VTime(0, 1, 0)), Event("b", VTime(1, 0, 0)));

            probe.ExpectMsg(ReplicationDue.Instance);
            probe.ExpectNoMsg(Timeout);
        }

        [Fact]
        public void Notification_channel_should_NOT_send_notification_if_update_is_in_casual_past_of_the_target()
        {
            SourceRead(TargetId1, VTime(0, 2, 0));
            SourceUpdate(Event("a", VTime(0, 1, 0)), Event("b", VTime(0, 2, 0)));
            
            probe.ExpectNoMsg(Timeout);
        }

        [Fact]
        public void Notification_channel_should_NOT_send_notification_if_update_doesnt_pass_target_ReplicationFilter()
        {
            SourceRead(TargetId1, VTime(0, 1, 0), e => Equals(e.Payload, "a"));
            SourceUpdate(Event("b", VTime(1, 0, 0)));

            probe.ExpectNoMsg(Timeout);
        }


        [Fact]
        public void Notification_channel_should_NOT_send_notification_if_target_has_concurrent_ReplicationRead()
        {
            channel.Tell(SourceReadMsg(TargetId1, VTime(0, 1, 0)));
            SourceUpdate(Event("a", VTime(1, 0, 0)), Event("b", VTime(2, 0, 0)));

            probe.ExpectNoMsg(Timeout);
        }
        
        [Fact]
        public void Notification_channel_should_apply_target_version_update()
        {
            SourceRead(TargetId1, VTime(0, 1, 0));
            SourceUpdate(Event("a", VTime(0, 2, 0)));

            probe.ExpectMsg(ReplicationDue.Instance);
            probe.ExpectNoMsg(Timeout);

            ReplicaVersionUpdate(TargetId1, VTime(0, 2, 0));
            SourceUpdate(Event("a", VTime(0, 2, 0)));

            probe.ExpectNoMsg(Timeout);
        }

        [Fact]
        public void Notification_channel_should_send_notifications_to_many_targets()
        {
            SourceRead(TargetId1, VTime(0, 1, 0));
            SourceRead(TargetId2, VTime(0, 0, 1));
            SourceUpdate(Event("a", VTime(1, 0, 0)));

            probe.ExpectMsg(ReplicationDue.Instance);
            probe.ExpectMsg(ReplicationDue.Instance);
            probe.ExpectNoMsg(Timeout);
        }

        [Fact]
        public void Notification_channel_should_NOT_send_notification_after_registration_expired()
        {
            SourceRead(TargetId1, VectorTime.Zero);
            Thread.Sleep(TimeSpan.FromMilliseconds(5000));
            SourceUpdate(Event("a", VTime(1, 0, 0)));
            probe.ExpectNoMsg(Timeout);
        }

        [Fact]
        public void Notification_channel_should_send_notification_after_registration_expired_but_new_Replication_request_was_received()
        {
            SourceRead(TargetId1, VectorTime.Zero);
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            SourceRead(TargetId1, VectorTime.Zero);
            SourceUpdate(Event("a", VTime(1, 0, 0)));
            probe.ExpectMsg(ReplicationDue.Instance);
        }

        private void SourceRead(string targetId, VectorTime targetTime, ReplicationFilter filter = null, TestProbe p = null)
        {
            p = p ?? probe;
            channel.Tell(SourceReadMsg(targetId, targetTime, 1, filter, p));
            channel.Tell(SourceReadSuccessMsg(targetId, 2));
        }

        private ReplicationRead SourceReadMsg(string targetId, VectorTime targetTime, int correlationId = 1, ReplicationFilter filter = null, TestProbe p = null) => 
            new ReplicationRead(1, 10, 100, targetId, filter, (p??probe).Ref, targetTime, correlationId, TestActor);

        private ReplicationReadSuccess SourceReadSuccessMsg(string targetId, int correlationId = 1) =>
            new ReplicationReadSuccess(correlationId, ImmutableArray<DurableEvent>.Empty, 10, 9, targetId, VectorTime.Zero);

        private void SourceUpdate(params DurableEvent[] events) => 
            channel.Tell(new NotificationChannel.Updated(events.ToImmutableArray()));

        private void ReplicaVersionUpdate(string targetId, VectorTime targetTime, int correlationId = 1) =>
            channel.Tell(new ReplicationWrite(
                ImmutableArray<DurableEvent>.Empty, 
                ImmutableDictionary<string, ReplicationMetadata>.Empty.Add(targetId, new ReplicationMetadata(10, targetTime)),
                correlationId));

        private static DurableEvent Event(object payload, VectorTime time) =>
            new DurableEvent(payload, "emitter", null, new Versioning.Version(time));

        private static VectorTime VTime(long source, long target1, long target2) =>
            new VectorTime(
                new KeyValuePair<string, long>(SourceId, source),
                new KeyValuePair<string, long>(TargetId1, target1),
                new KeyValuePair<string, long>(TargetId2, target2));
    }
}