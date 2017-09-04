using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calliope.Sql
{
    public interface ISqlDialect<TRow> where TRow : IEventRow
    {
        Task Initialize();
        Task WriteBatch(TRow[] rows);
        Task<IEnumerator<TRow>> ReadRows(long from, long to, int limit);
        Task Delete(long to);
    }
}