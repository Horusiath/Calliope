using System;
using Akka.Actor;
using Calliope.Persistence.Journals;
using Calliope.Persistence.Snapshots;
using Calliope.Replication;

namespace Calliope
{
    public class Calliope: IExtension
    {
        public static Calliope Get(ActorSystem system) => system.WithExtension<Calliope, CalliopeProvider>();

        public Calliope(ExtendedActorSystem system)
        {
            
        }

        public IEventJournal GetEventJournal(string journalId)
        {
            throw new NotImplementedException();
        }

        public ISnapshotStore GetSnapshotStore(string snapshotStoreId)
        {
            throw new NotImplementedException();
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