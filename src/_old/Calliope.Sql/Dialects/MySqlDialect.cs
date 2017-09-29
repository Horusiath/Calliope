using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calliope.Sql.Dialects
{
    public class MySqlDialect<TRow> : ISqlDialect<TRow> where TRow : IEventRow
    {
        public Task Initialize()
        {
            throw new System.NotImplementedException();
        }

        public Task WriteBatch(TRow[] rows)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerator<TRow>> ReadRows(long @from, long to, int limit)
        {
            throw new System.NotImplementedException();
        }

        public Task Delete(long to)
        {
            throw new System.NotImplementedException();
        }
    }
}