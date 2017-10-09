#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ReplicatorManager.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using Akka.Actor;

namespace Calliope.Replication
{
    /// <summary>
    /// A parent actor for all <see cref="Replicator{T}"/> classes. Replicators are dedicated
    /// to replicate messages comming within a context of a particular topic.
    /// </summary>
    internal sealed class ReplicatorManager : ReceiveActor
    {
        private readonly ReplicatorSettings settings;

        public ReplicatorManager(ReplicatorSettings settings)
        {
            this.settings = settings;
            

        }

        private IActorRef GetOrCreateTopic<T>(string topicId)
        {
            var replicatorRef = Context.Child(topicId);
            if (Equals(replicatorRef, ActorRefs.Nobody))
            {
                var props = Props.Create(() => new Replicator<T>(topicId, settings));
                replicatorRef = Context.ActorOf(props, topicId);
            }

            return replicatorRef;
        }
    }
}