namespace Calliope.Persistence.Journals
{
    public interface IEventJournal
    {
        /// <summary>
        /// Gets an event stream connection object for a provided <paramref name="streamId"/>.
        /// There could be only one writer for that stream at the time - concurrent writes 
        /// are not allowed.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="streamId"></param>
        /// <returns></returns>
        IEventStream<TEvent> GetEventStream<TEvent>(string streamId);

        /// <summary>
        /// Gets an event stream connection object for a provided <paramref name="streamId"/>.
        /// While there can be multiple concurrent writers for that stream, each one of them
        /// must have its own unique <paramref name="replicaId"/>.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="streamId"></param>
        /// <param name="replicaId"></param>
        /// <returns></returns>
        IReplicatedEventStream<TEvent> GetReplicatedEventStream<TEvent>(string streamId, string replicaId);
    }
}