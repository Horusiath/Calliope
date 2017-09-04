using Calliope.Persistence.Journals;

namespace Calliope.Sql
{
    public class SqlJournal<TDialect, TRow> : IEventJournal 
        where TDialect : ISqlDialect<TRow> 
        where TRow : IEventRow
    {
        public IEventStream<TEvent> GetEventStream<TEvent>(string streamId)
        {
            throw new System.NotImplementedException();
        }

        public IReplicatedEventStream<TEvent> GetReplicatedEventStream<TEvent>(string streamId, string replicaId)
        {
            throw new System.NotImplementedException();
        }
    }
}