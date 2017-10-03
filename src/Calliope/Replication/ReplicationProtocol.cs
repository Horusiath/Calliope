#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ReplicationProtocol.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;

namespace Calliope.Replication
{
    public struct Durable<T>
    {
        public string ChannelId { get; }
        public long SequenceNr { get; }
        public Versioned<T> Versioned { get; }

        public Durable(string channelId, long sequenceNr, Versioned<T> versioned)
        {
            ChannelId = channelId;
            SequenceNr = sequenceNr;
            Versioned = versioned;
        }
    }

    internal interface IReplicatorStore
    {
        Source<Durable<T>, NotUsed> Replay<T>(string channelId);
        Task Store<T>(Versioned<T> update);
    }
}