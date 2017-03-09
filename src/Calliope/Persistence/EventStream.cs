using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Calliope.Replication;
using Calliope.Versioning;

namespace Calliope.Persistence
{
    public class EventStream<TState, TEvent> : IEventStream<TState, TEvent>
    {
        private readonly CorrelatableRef journalProxy;
        private readonly CorrelatableRef snapshotStoreProxy;
        private VersionClock currentVersion;
        private DurableEvent lastEvent;

        private HashSet<SnapshotMetadata> saveRequests = new HashSet<SnapshotMetadata>();
        

        internal EventStream()
        {

        }

        #region IEventStream implementation

        public string StreamId { get; }
        public string ReplicaId { get; }

        public ReplicationEndpoint ReplicationEndpoint { get; }
        public Task<TState> LoadSnapshot(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new System.NotImplementedException();
        }

        public async Task SaveSnapshot(TState snapshot, CancellationToken cancellationToken = default(CancellationToken))
        {
            var metadata = new SnapshotMetadata(ReplicaId, lastEvent.Version.SequenceNr);
            if (!saveRequests.Add(metadata))
            {
                throw new SaveSnapshotException($"Snapshot with metadata {metadata} is currently being saved");
            }
            else
            {
                var prototype = new Snapshot(snapshot, metadata, lastEvent.Version);
            }
        }

        public Task<TState> Recover(Update<TState, TEvent> update, TState initialState = default(TState), CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new System.NotImplementedException();
        }

        public Task Persist(TEvent e, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new System.NotImplementedException();
        }

        public Task Persist(IEnumerable<TEvent> events, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}