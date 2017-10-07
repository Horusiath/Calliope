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
    using ReplicaId = Int32;

    public interface IEventRow
    {
        string StreamId { get; }
        ReplicaId ReplicaId { get; }
        VClock Timestamp { get; }
    }

    public sealed class PayloadRow : IEventRow
    {
        public string StreamId { get; }
        public ReplicaId ReplicaId { get; }
        public VClock Timestamp { get; }
        public int SerializerId { get; }
        public byte[] Payload { get; }

        public PayloadRow(string streamId, ReplicaId replicaId, VClock timestamp, int serializerId, byte[] payload)
        {
            StreamId = streamId;
            ReplicaId = replicaId;
            Timestamp = timestamp;
            SerializerId = serializerId;
            Payload = payload;
        }
    }

    public sealed class JsonRow : IEventRow
    {
        public string StreamId { get; }
        public ReplicaId ReplicaId { get; }
        public VClock Timestamp { get; }
        public string JsonPayload { get; }

        public JsonRow(string streamId, ReplicaId replicaId, VClock timestamp, string jsonPayload)
        {
            StreamId = streamId;
            ReplicaId = replicaId;
            Timestamp = timestamp;
            JsonPayload = jsonPayload;
        }
    }
}