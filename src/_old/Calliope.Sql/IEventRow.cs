using System;
using System.Collections.Generic;

namespace Calliope.Sql
{
    public interface IEventRow
    {
        string StreamId { get; }
        string ReplicaId { get; }
        DateTime SystemTime { get; }
        IReadOnlyDictionary<string, ulong> VectorTime { get; }
    }

    public sealed class PayloadRow : IEventRow
    {
        public string StreamId { get; }
        public string ReplicaId { get; }
        public DateTime SystemTime { get; }
        public IReadOnlyDictionary<string, ulong> VectorTime { get; }
        public int SerializerId { get; }
        public byte[] Payload { get; }

        public PayloadRow(string streamId, string replicaId, DateTime systemTime, IReadOnlyDictionary<string, ulong> vectorTime, int serializerId, byte[] payload)
        {
            StreamId = streamId;
            ReplicaId = replicaId;
            SystemTime = systemTime;
            VectorTime = vectorTime;
            SerializerId = serializerId;
            Payload = payload;
        }
    }

    public sealed class JsonRow : IEventRow
    {
        public string StreamId { get; }
        public string ReplicaId { get; }
        public DateTime SystemTime { get; }
        public IReadOnlyDictionary<string, ulong> VectorTime { get; }
        public string JsonPayload { get; }

        public JsonRow(string streamId, string replicaId, DateTime systemTime, IReadOnlyDictionary<string, ulong> vectorTime, string jsonPayload)
        {
            StreamId = streamId;
            ReplicaId = replicaId;
            SystemTime = systemTime;
            VectorTime = vectorTime;
            JsonPayload = jsonPayload;
        }
    }
}