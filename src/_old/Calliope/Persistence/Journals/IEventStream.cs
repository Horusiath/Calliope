using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;

namespace Calliope.Persistence.Journals
{
    public interface IEventStream<TEvent>
    {
        /// <summary>
        /// An event stream identifier.
        /// </summary>
        string StreamId { get; }

        /// <summary>
        /// Performs a full query of currennt event stream. Stream read will
        /// stop automatically when there are no more events to read.
        /// </summary>
        Source<DurableEvent<TEvent>, NotUsed> Query(long from = 0, long to = long.MaxValue, long limit = long.MaxValue, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Performs a full recovery of currennt event stream. Stream will
        /// continue activelly awaiting for the upcoming events until it 
        /// will be explicitly disposed.
        /// </summary>
        Source<DurableEvent<TEvent>, IDisposable> LiveQuery(long from = 0, long to = long.MaxValue, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Persists a current event under provided event stream.
        /// </summary>
        Task<DurableEvent<TEvent>> Persist(TEvent e, CancellationToken cancellationToken = default(CancellationToken));
        
        /// <summary>
        /// Permanently deletes all known events up to the provided version number.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteTo(long to = long.MaxValue, CancellationToken cancellationToken = default(CancellationToken));
    }
}