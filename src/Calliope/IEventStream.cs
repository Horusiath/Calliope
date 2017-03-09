using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Calliope.Replication;

namespace Calliope
{
    public delegate TState Update<TState, in TEvent>(TState state, TEvent e);

    public interface IEventStream<TState, TEvent>
    {
        /// <summary>
        /// An optional event stream identifier.
        /// </summary>
        string StreamId { get; }

        /// <summary>
        /// An identifier of a current stream replica.
        /// </summary>
        string ReplicaId { get; }

        /// <summary>
        /// Replication endpoint used to replicate data across the cluster 
        /// between many different nodes, possibly containing other replicas 
        /// of the same event stream.
        /// </summary>
        ReplicationEndpoint ReplicationEndpoint { get; }

        /// <summary>
        /// Loads the latest possible snapshot of current event stream.
        /// </summary>
        Task<TState> LoadSnapshot(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Saves the <paramref name="snapshot"/> of the current event stream.
        /// </summary>
        Task SaveSnapshot(TState snapshot, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Performs a full recovery of currennt event stream. It starts from 
        /// loading the latests known snapshot and forwarding from all events 
        /// onwerd. Returns a result of applying an <paramref name="update"/> 
        /// function on all incoming events on a state.
        /// 
        /// If no state could be loaded, the <paramref name="initialState"/>
        /// will be used.
        /// </summary>
        Task<TState> Recover(Update<TState, TEvent> update, TState initialState = default(TState), CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Persists a current event under provided event stream.
        /// </summary>
        Task Persist(TEvent e, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Persists many events under provided event stream in one batch.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        Task Persist(IEnumerable<TEvent> events, CancellationToken cancellationToken = default(CancellationToken));
    }
}