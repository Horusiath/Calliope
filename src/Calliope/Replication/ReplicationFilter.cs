using Calliope.Persistence;

namespace Calliope.Replication
{
    /// <summary>
    /// Run filter check against the given <paramref name="event"/>.
    /// </summary>
    public delegate bool ReplicationFilter(DurableEvent @event);
}