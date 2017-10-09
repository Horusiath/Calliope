#region copyright
// -----------------------------------------------------------------------
//  <copyright file="ISnapshotStore.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Threading.Tasks;

namespace Calliope.Persistence
{
    public interface ISnapshotStore<T>
    {
        Task<SnapshotOffer<T>> LoadAsync();
        Task<SnapshotOffer<T>> LoadAsync(SnapshotSelectionCriteria criteria);
        Task SaveAsync(T snapshot);
    }
}