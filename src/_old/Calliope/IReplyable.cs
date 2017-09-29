using Akka.Actor;

namespace Calliope
{
    public interface IReplyable<TReply> where TReply : IReply
    {
        int CorrelationId { get; }
        IActorRef ReplyTo { get; }
    }

    public interface IReply
    {
        int CorrelationId { get; }
    }
}