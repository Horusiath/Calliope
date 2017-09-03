using System;
using Akka.Streams.Dsl;
using System.Threading;
using System.Threading.Tasks;

namespace Calliope.Replication
{
    public interface IReplicator
    {
        /// <summary>
        /// Gets a current value stored under provided key. Number of replicas 
        /// may be provided in order to resolve the state from more than a 
        /// single node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="replicas"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> Get<T>(string key, int replicas = 1, CancellationToken cancellationToken = default(CancellationToken)) where T: class;

        /// <summary>
        /// Updates a CRDT value under provided <paramref name="key"/>.
        /// If no value were stored before, a new empty one will be initialized.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="replicas"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Update<T>(string key, T value, int replicas = 1, CancellationToken cancellationToken = default (CancellationToken));

        /// <summary>
        /// Marks a value within a provided <paramref name="key"/> as
        /// deleted. No more updates for this value will be allowed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Delete(string key, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns a stream of updates incoming for a provided 
        /// <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Source<ValueUpdated<T>, IDisposable> Updates<T>(string key);

        /// <summary>
        /// Skips current thread until a provided CRDT value stored under
        /// provided <paramref name="key"/> will pass expected <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="predicate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WaitUntil<T>(string key, Func<T, bool> predicate, CancellationToken cancellationToken = default (CancellationToken));


    }
}