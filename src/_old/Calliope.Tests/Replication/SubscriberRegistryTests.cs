using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Calliope.Persistence;
using Calliope.Replication.Notifications;
using Calliope.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace Calliope.Tests.Replication
{
    public class SubscriberRegistryTests : TestKit
    {
        private readonly TestProbe a1;
        private readonly TestProbe a2;
        private readonly TestProbe b1;
        private readonly TestProbe d1;

        public SubscriberRegistryTests(ITestOutputHelper output) : base(output: output)
        {
            a1 = CreateTestProbe("a1");
            a2 = CreateTestProbe("a2");
            b1 = CreateTestProbe("b1");
            d1 = CreateTestProbe("d1");
        }

        [Fact]
        public void SubscriberRegistry_should_allow_to_notify_registered_subscribers()
        {
            var registry = new SubscriberRegistry()
                .Register(a1.Ref, "a")
                .Register(a2.Ref, "a");

            var events = MakeEvents("a", 1);
            registry.Notify(events);

            a1.ExpectMsg(new Written(events[0]));
            a2.ExpectMsg(new Written(events[0]));
        }

        [Fact]
        public void SubscriberRegistry_should_not_notify_subscribers_registered_for_different_streamId()
        {
            var registry = new SubscriberRegistry()
                .Register(b1.Ref, "b");

            var events = MakeEvents("a", 1);
            registry.Notify(events);

            b1.ExpectNoMsg();
        }

        [Fact]
        public void SubscriberRegistry_should_always_notify_default_subscribers()
        {
            var registry = new SubscriberRegistry()
                .RegisterDefault(d1.Ref);

            var events = MakeEvents("a", 1);
            registry.Notify(events);

            d1.ExpectMsg(new Written(events[0]));

            events = MakeEvents("b", 1);
            registry.Notify(events);

            d1.ExpectMsg(new Written(events[0]));
        }

        [Fact]
        public void SubscriberRegistry_should_not_notify_unregistered_subscribers()
        {
            var registry = new SubscriberRegistry()
                .Register(a1.Ref, "a")
                .Register(a2.Ref, "a")
                .RegisterDefault(d1.Ref)
                .Unregister(a2.Ref)
                .Unregister(d1.Ref);

            var events = MakeEvents("a", 1);
            registry.Notify(events);

            a1.ExpectMsg(new Written(events[0]));
            a2.ExpectNoMsg();
            d1.ExpectNoMsg();
        }

        [Fact]
        public void SubscriberRegistry_should_filter_both_subscribers_types_if_filter_was_given()
        {
            var registry = new SubscriberRegistry()
                .Register(a1.Ref, "a")
                .Register(a2.Ref, "a")
                .RegisterDefault(d1);

            var events = MakeEvents("a", 1);
            registry.Notify(events, actorRef => actorRef.Path.Name.EndsWith("2"));

            a2.ExpectMsg(new Written(events[0]));
            a1.ExpectNoMsg();
            d1.ExpectNoMsg();
        }

        private DurableEvent[] MakeEvents(string streamId, params int[] payloads)
        {
            var events = new DurableEvent[payloads.Length];
            for (int i = 0; i < payloads.Length; i++)
            {
                var payload = payloads[i];
                var replicaId = streamId + i;
                var time = VectorTime.Create(replicaId);
                var e = new DurableEvent(payload, replicaId, streamId, new Version(time));
                events[i] = e;
            }

            return events;
        }
    }
}