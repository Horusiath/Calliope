using System;
using Akka;
using Akka.Streams.Dsl;

namespace Calliope.Replication
{
    public sealed class BroadcastSettings
    {
        public ReplicationNetwork ReplicationNetwork { get; }
        public IPersistentBackplane Persistence { get; }
    }

    public static class Broadcast
    {
        /// <summary>
        /// Creates a broadcast protocol receiver, which is able to
        /// perform reliable message dissemination. This means that 
        /// provided messages are always guaranteed to be delivered
        /// using exactly once delivery mechanisms.
        /// 
        /// For that to happen, a persistent backend should be 
        /// provided.
        /// </summary>
        /// <seealso cref="TaggedReliableCasualBroadcast{T}"/>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Sink<T, NotUsed> ReliableCasualBroadcast<T>(BroadcastSettings settings = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a broadcast protocol receiver, which is able to
        /// perform reliable message dissemination. This means that 
        /// provided messages are always guaranteed to be delivered
        /// using exactly once delivery mechanisms.
        /// 
        /// For that to happen, a persistent backend should be 
        /// provided. In addition to guarantees provided by the 
        /// <see cref="ReliableCasualBroadcast{T}"/>, this protocol
        /// is also able to perform concurrent messages resolution.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Sink<T, NotUsed> TaggedReliableCasualBroadcast<T>(BroadcastSettings settings = null)
        {
            throw new NotImplementedException();
        }
    }
}