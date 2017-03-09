using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;

namespace Calliope
{
    internal sealed class CorrelatableRef : MinimalActorRef
    {
        private readonly ConcurrentDictionary<int, TaskCompletionSource<object>> completions = 
            new ConcurrentDictionary<int, TaskCompletionSource<object>>();

        public override ActorPath Path { get; }
        public override IActorRefProvider Provider { get { throw new NotImplementedException();} }

        public CorrelatableRef(ActorPath path)
        {
            Path = path;
        }

        public async Task<TRep> PostAndAwait<TReq, TRep>(IActorRef recipient, TReq request, CancellationToken token = default(CancellationToken))
            where TReq : IReplyable<TRep>
            where TRep : IReply
        {
            var correlationId = request.CorrelationId;
            recipient.Tell(request, this);
            var completion = new TaskCompletionSource<object>();
            if (completions.TryAdd(correlationId, completion))
            {
                token.Register(() =>
                {
                    TaskCompletionSource<object> c;
                    if (completions.TryRemove(request.CorrelationId, out c))
                    {
                        c.TrySetCanceled();
                    }
                });
                return (TRep) (await completion.Task);
            }

            throw new CorrelationException($"Another request with correlation id '{correlationId}' is already pending", 
                correlationId);
        }

        protected override void TellInternal(object message, IActorRef sender)
        {
            var reply = message as IReply;
            if (reply != null)
            {
                TaskCompletionSource<object> completion;
                if (completions.TryRemove(reply.CorrelationId, out completion))
                {
                    completion.SetResult(message);
                }
            }
            else base.TellInternal(message, sender);
        }

        public override void Stop()
        {
            base.Stop();
            foreach (var entry in completions)
            {
                entry.Value.TrySetCanceled();
            }
        }
    }
}