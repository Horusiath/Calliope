#region copyright
// -----------------------------------------------------------------------
//  <copyright file="IEventLog.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;

namespace Calliope.Persistence
{
    public interface IEventLog<TEvent>
    {
        Source<Durable<TEvent>, NotUsed> Query(long from = 0, long to = long.MaxValue, int max = int.MaxValue);
        Source<Durable<TEvent>, NotUsed> LiveQuery(long from = 0, CancellationToken token = default(CancellationToken));

        Task PersistAsync(TEvent e, CancellationToken token = default(CancellationToken));
    }
}
