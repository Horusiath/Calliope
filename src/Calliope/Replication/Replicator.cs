#region copyright
// -----------------------------------------------------------------------
//  <copyright file="Replicator.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;

namespace Calliope.Replication
{
    public static class Replicator
    {
        public interface IIncoming<T> { }


        public interface IOutgoing<T> { }

        /// <summary>
        /// Creates a replicator flow that will be able to send messages using Tagged Reliable Casual Broadcast protocol.
        /// This flow will reliably deliver all input messages of type <typeparamref name="T"/> and produce a 
        /// <see cref="Versioned{T}"/> events as the ones casually delivered to current replica.
        /// 
        /// In order to behave reliably, this bidi flow requires to be set atop of eventsourced flow.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamId"></param>
        /// <param name="replicaId"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static BidiFlow<IOutgoing<T>, object, object, IIncoming<T>, NotUsed> ReliableCasualBroadcast<T>(string streamId, string replicaId, ReplicatorSettings settings = null) =>
            throw new NotImplementedException();
    }

    internal sealed class Replicator<T> : GraphStage<BidiShape<T, /* Replicator state changes */ object, /* Replicator state change confirmations */ object, Versioned<T>>>
    {
        public override BidiShape<T, object, object, Versioned<T>> Shape { get; }
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes)
        {
            throw new System.NotImplementedException();
        }
    }
}