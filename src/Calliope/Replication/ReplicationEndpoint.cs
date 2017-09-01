using System;
using Akka.Actor;

namespace Calliope.Replication
{
    public struct ReplicationEndpoint : IEquatable<ReplicationEndpoint>
    {
        public static readonly ReplicationEndpoint None = new ReplicationEndpoint(string.Empty, Address.AllSystems);

        public string EndpointId { get; }
        public Address Address { get; }

        public ReplicationEndpoint(string endpointId, Address address)
        {
            EndpointId = endpointId;
            Address = address;
        }

        public bool Equals(ReplicationEndpoint other)
        {
            return string.Equals(EndpointId, other.EndpointId) && Equals(Address, other.Address);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ReplicationEndpoint && Equals((ReplicationEndpoint) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((EndpointId != null ? EndpointId.GetHashCode() : 0) * 397) ^ (Address != null ? Address.GetHashCode() : 0);
            }
        }

        public override string ToString() => $"Endpoint({EndpointId}, {Address})";
    }

    public sealed class ReplicationEdge
    {
        public ReplicationEndpoint Source { get; }
        public ReplicationEndpoint Target { get; }

        public ReplicationEdge(ReplicationEndpoint source, ReplicationEndpoint target)
        {
            Source = source;
            Target = target;
        }
    }
}