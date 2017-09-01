using System;
using Akka;
using Akka.Streams.Dsl;

namespace Calliope.Replication
{
    public sealed class BroadcastSettings
    {
        public ReplicationNetwork ReplicationNetwork { get; }
        
    }

    public static class Broadcast
    {
        public static Flow<IReplicationRequest, IReplicationReply, NotImplementedException> Create(BroadcastSettings settings)
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
        /// provided.
        /// </summary>
        /// <seealso cref="Tagged{T}"/>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static BidiFlow<object, IReplicationRequest, IReplicationReply, object, NotUsed> ReliableCasual<T>()
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
        /// <see cref="ReliableCasual{T}"/>, this protocol
        /// is also able to perform concurrent messages resolution.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static BidiFlow<object, IReplicationRequest, IReplicationReply, object, NotUsed> Tagged<T>()
        {
            throw new NotImplementedException();
        }
    }
}