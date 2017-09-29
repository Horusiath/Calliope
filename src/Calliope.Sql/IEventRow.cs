#region copyright
// -----------------------------------------------------------------------
//  <copyright file="IEventRow.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;

namespace Calliope.Sql
{
    public interface IEventRow
    {
        string StreamId { get; }
        string ReplicaId { get; }
        DateTime SystemTimestamp { get; }
        IReadOnlyDictionary<string, ulong> VectorTimestamp { get; }
    }

    public sealed class PayloadRow : IEventRow
    {
        public string StreamId { get; }
        public string ReplicaId { get; }
        public DateTime SystemTimestamp { get; }
        public IReadOnlyDictionary<string, ulong> VectorTimestamp { get; }
        public int SerializerId { get; }
        public byte[] Payload { get; }

        public PayloadRow(string streamId, string replicaId, DateTime systemTimestamp, IReadOnlyDictionary<string, ulong> vectorTimestamp, int serializerId, byte[] payload)
        {
            StreamId = streamId;
            ReplicaId = replicaId;
            SystemTimestamp = systemTimestamp;
            VectorTimestamp = vectorTimestamp;
            SerializerId = serializerId;
            Payload = payload;
        }
    }

    public sealed class JsonRow : IEventRow
    {
        public string StreamId { get; }
        public string ReplicaId { get; }
        public DateTime SystemTimestamp { get; }
        public IReadOnlyDictionary<string, ulong> VectorTimestamp { get; }
        public string JsonPayload { get; }

        public JsonRow(string streamId, string replicaId, DateTime systemTimestamp, IReadOnlyDictionary<string, ulong> vectorTimestamp, string jsonPayload)
        {
            StreamId = streamId;
            ReplicaId = replicaId;
            SystemTimestamp = systemTimestamp;
            VectorTimestamp = vectorTimestamp;
            JsonPayload = jsonPayload;
        }
    }
}