using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Calliope.Persistence;

namespace Calliope.Replication.Notifications
{
    internal sealed class SubscriberRegistry
    {
        private readonly Registry<string, IActorRef> registry;
        private readonly ImmutableHashSet<IActorRef> defaultRegistry;

        public SubscriberRegistry(
            Registry<string, IActorRef> registry = null,
            ImmutableHashSet<IActorRef> defaultRegistry = null)
        {
            this.registry = registry ?? new Registry<string, IActorRef>();
            this.defaultRegistry = defaultRegistry ?? ImmutableHashSet<IActorRef>.Empty;
        }

        public SubscriberRegistry Register(IActorRef subscriber, string eventStreamId) =>
            new SubscriberRegistry(
                registry: this.registry.Add(eventStreamId, subscriber),
                defaultRegistry: this.defaultRegistry);

        public SubscriberRegistry RegisterDefault(IActorRef subscriber) =>
            new SubscriberRegistry(
                registry: this.registry,
                defaultRegistry: this.defaultRegistry.Add(subscriber));

        public SubscriberRegistry Unregister(IActorRef subscriber)
        {
            var eventStreamId = this.registry[subscriber];
            return eventStreamId != null
                ? new SubscriberRegistry(
                    registry: this.registry.Remove(eventStreamId, subscriber),
                    defaultRegistry: this.defaultRegistry)
                : new SubscriberRegistry(
                    registry: this.registry,
                    defaultRegistry: this.defaultRegistry.Remove(subscriber));
        }

        public void Notify(IEnumerable<DurableEvent> events, Func<IActorRef, bool> predicate = null)
        {
            var defaults = predicate != null
                ? this.defaultRegistry.Where(predicate).ToImmutableHashSet()
                : this.defaultRegistry;

            foreach (var e in events)
            {
                var written = new Written(e);
                foreach (var subscriber in defaults)
                    subscriber.Tell(written);

                var subscribers = predicate != null
                    ? this.registry[e.StreamId].Where(predicate)
                    : this.registry[e.StreamId];

                foreach (var subscriber in subscribers)
                    subscriber.Tell(written);
            }
        }
    }
}