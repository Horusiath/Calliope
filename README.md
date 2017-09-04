# Calliope

Calliope is another approach to eventsourcing and replication in Akka.NET. It aims to provide a replicable event-based state management using Akka.Streams API.

## Goals

- In Akka.Persistence in order to keep consistent state, persistent actors must guarantee to be a global singleton. We want to be able to relax that consistency constraint and be able to introduce casual consistency.
- We want to simplify Akka.Persistence API, by utilizing possibilities given by Akka.Streams.
- We want to introduce operation-based CRDTs, that could be used as casual source of data.
- Composition over inheritance.
- Make API suitable for being used outside of actor pattern.
- Use known .NET primitives (Tasks, async/await) for non-blocking I/O.

Terms:

- Snapshot store - component used for storing state snapshot in the persistent database.
- Event stream   - a logical stream of events associated with a single aggregate (identifier by `streamId`).
- Replicated event stream - just like event stream, but it can also be spanned by multiple instances of a single aggregate, writing to an event store concurrently (individual instances are recognized by their `replicaId`).

## API 

Experimental proposal for an API:

#### Event stream

```csharp
var events = eventJournal.GetEventStream<IDomainEvent>(streamId);

// replay current events i.e. for actor recovery, using Akka.Streams source
this.State = await events.Query()
    .RunAggregate(UpdateState, default(State), materializer);

// an infinite event stream, filled as new events come in i.e. for projections
IDisposable liveQuery = events.LiveQuery().RunWith(materializer);
liveQuery.Dispose(); // dispose live query

// persist an event
await events.Persist(new ItemCreated(), cancellationToken);
```

#### Replicated event stream

```csharp
var events = eventJournal.GetReplicatedEventStream<IDomainEvent>(streamId, replicaId);

// persist an event
var reply = await events.Persist(new ItemCreated(), cancellationToken);
if (reply.ConcurrentUpdateDetected)
{
    ResolveConflicts(this.State, reply.ConcurrentUpdates);
}
```

#### CRDT replicator

```csharp
var replicator = Calliope.Get(system).Replicator;

// get CRDT for a key
var value = await replicator.Get<GCounter>("my-key", replicas: 3, cancellationToken);

// update CRDT for a key
await replicator.Update("my-key", gcounter, replicas: 3, cancellationToken);

// get stream of changes for a provided CRDT
await replicator.Updates("my-key")
    .RunForEach(update => Console.WriteLine($"{update}), materializer);

// conditionally wait (non-blocking) until a provided condition is satisfied
await replicator.WaitUntil("my-key", gcounter => gcounter.Value >= 5000, cancellationToken);
```

#### Snapshot store

```csharp
var store = Calliope.Get(system).GetSnapshotStore();

// save snapshot
await store.SaveSnapshot(snapshotId, this.State, cancellationToken);

// load snapshot
var value = await store.LoadSnapshot(snapshotId, cancellationToken);
this.State = value.State;
```
