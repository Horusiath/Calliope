using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;

namespace Calliope.Persistence.Journals
{
    public interface IReplicatedEventStream<TEvent>
    {
        /// <summary>
        /// An event stream identifier.
        /// </summary>
        string StreamId { get; }

        /// <summary>
        /// An optional identifier of a current stream replica.
        /// </summary>
        string ReplicaId { get; }

        /// <summary>
        /// Performs a full query of currennt event stream. Stream read will
        /// stop automatically when there are no more events to read.
        /// </summary>
        Source<DurableEvent<TEvent>, NotUsed> Query(Version from, Version to, long limit = long.MaxValue, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Performs a full recovery of currennt event stream. Stream will
        /// continue activelly awaiting for the upcoming events until it 
        /// will be explicitly disposed.
        /// </summary>
        Source<DurableEvent<TEvent>, IDisposable> LiveQuery(Version from, Version to, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Persists a current event under provided event stream.
        /// </summary>
        Task<PersistResult<TEvent>> Persist(TEvent e, CancellationToken cancellationToken = default(CancellationToken));
    }
}