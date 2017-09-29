using System.Threading;
using System.Threading.Tasks;

namespace Calliope.Persistence.Snapshots
{
    public interface ISnapshotStore
    {
        /// <summary>
        /// Loads the latest possible snapshot of current event stream.
        /// </summary>
        Task<TState> LoadSnapshot<TState>(string snapshotId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Saves the <paramref name="snapshot"/> of the current event stream.
        /// </summary>
        Task SaveSnapshot<TState>(string snapshotId, TState snapshot, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Permanently deletes the latest possible snapshot of current event stream.
        /// </summary>
        Task DeleteSnapshot(string snapshotId, CancellationToken cancellationToken = default(CancellationToken));
    }
}