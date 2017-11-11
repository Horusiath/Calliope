#region copyright
// -----------------------------------------------------------------------
//  <copyright file="Calliope.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using Calliope.Replication;

namespace Calliope
{
    public sealed class Calliope : IExtension
    {
        public static Calliope Get(ActorSystem system) => system.WithExtension<Calliope, CalliopeProvider>();

        private readonly ExtendedActorSystem system;

        public Calliope(ExtendedActorSystem system)
        {
            this.system = system;
        }

        public IActorRef TopicRef<T>(string topic)
        {
            var replicaId = system.Provider.DefaultAddress.ToString() + topic;
            return system.ActorOf(ReplicatorActor<T>.Props(replicaId, ReplicatorSettings.Default.WithRole("calliope")));
        }

        public Flow<T, Versioned<T>, NotUsed> Topic<T>(string topic)
        {
            return Flow.FromGraph(new ReplicatorStage<T>());
        }

        public Sink<T, NotUsed> TopicPublisher<T>(string topic)
        {
            return Topic<T>(topic).To(Sink.Ignore<Versioned<T>>());
        }
    }

    internal sealed class CalliopeProvider : ExtensionIdProvider<Calliope>
    {
        public override Calliope CreateExtension(ExtendedActorSystem system)
        {
            return new Calliope(system);
        }
    }
}