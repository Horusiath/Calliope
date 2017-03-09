using System;
using System.Collections.Immutable;
using Akka.Actor;

namespace Calliope.Replication
{
    /// <summary>
    /// Replication endpoint manages a collection of journals grouped into a single node 
    /// of a replication network. Events are replicated between endpoints via so-called
    /// <see cref="ReplicationConnection"/>s. Connections are uni-directional.
    /// 
    /// A single endpoint may manage multiple event journals, which are isolated from each other.
    /// </summary>
    public class ReplicationEndpoint
    {
        /// <summary>
        /// Unique identifier of an endpoint in scope of a current replication network.
        /// </summary>
        public string EndpointId { get; }

        /// <summary>
        /// Identifiers of the event journals managed by this endpoint.
        /// </summary>
        public ImmutableHashSet<string> Journals { get; }

        /// <summary>
        /// A factory, which is able to generate an event journal <see cref="Props"/> for 
        /// a given event journal id.
        /// </summary>
        public Func<string, Props> JournalFactory { get; }

        /// <summary>
        /// Connections to other replication endpoints.
        /// </summary>
        public ImmutableHashSet<Address> Connections { get; }

        public ReplicationEndpoint(string endpointId, ImmutableHashSet<string> journals, Func<string, Props> journalFactory, ImmutableHashSet<Address> connections)
        {
            EndpointId = endpointId;
            Journals = journals;
            JournalFactory = journalFactory;
            Connections = connections;
        }
    }
}