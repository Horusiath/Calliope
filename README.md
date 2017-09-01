# Calliope

Calliope is another approach to eventsourcing and replication in Akka.NET. It aims to provide a replicable event-based state management using Akka.Streams API.

## Goals

- In Akka.Persistence in order to keep consistent state, persistent actors must guarantee to be a global singleton. We want to be able to relax that consistency constraint and be able to introduce casual consistency.
- We want to simplify Akka.Persistence API, by utilizing possibilities given by Akka.Streams.
- We want to introduce operation-based CRDTs, that could be used as casual source of data.

## API 

```csharp

```