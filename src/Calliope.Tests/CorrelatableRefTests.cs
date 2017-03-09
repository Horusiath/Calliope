using System;
using System.Threading;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Calliope.Tests
{
    public class CorrelatableRefTests : TestKit
    {
        #region internal classes

        private class Request : IReplyable<Reply> 
        {
            public object Payload { get; }
            public int CorrelationId { get; }
            public IActorRef ReplyTo { get; }

            public Request(object payload, int correlationId, IActorRef replyTo)
            {
                Payload = payload;
                CorrelationId = correlationId;
                ReplyTo = replyTo;
            }
        }

        private class Reply : IReply, IEquatable<Reply>
        {
            public object Payload { get; }
            public int CorrelationId { get; }

            public Reply(object payload, int correlationId)
            {
                Payload = payload;
                CorrelationId = correlationId;
            }

            public bool Equals(Reply other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Payload, other.Payload) && CorrelationId == other.CorrelationId;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Reply) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Payload != null ? Payload.GetHashCode() : 0) * 397) ^ CorrelationId;
                }
            }
        }

        class Echo : ReceiveActor
        {
            public Echo()
            {
                Receive<Request>(req => Sender.Tell(new Reply(req.Payload, req.CorrelationId)));
            }
        }

        #endregion

        private readonly CorrelatableRef cref;
        private readonly IActorRef target;

        public CorrelatableRefTests(ITestOutputHelper output) : base(output: output)
        {
            cref = new CorrelatableRef(new RootActorPath(new Address("akka.tcp", Sys.Name)) / "user" / "correlatable-ref");
            target = Sys.ActorOf(Props.Create<Echo>(), "echo");
        }

        [Fact]
        public void CorrelatableRef_should_allow_to_await_on_correlated_reply()
        {
            var reply = cref.PostAndAwait<Request, Reply>(target, Req("a", 1)).Result;
            reply.Should().Be(new Reply("a", 1));
        }


        [Fact]
        public void CorrelatableRef_should_allow_to_await_on_many_correlated_replies()
        {
            var t1 = cref.PostAndAwait<Request, Reply>(target, Req("a", 1));
            var t2 = cref.PostAndAwait<Request, Reply>(target, Req("b", 2));
            var t3 = cref.PostAndAwait<Request, Reply>(target, Req("c", 3));

            t3.Result.Should().Be(new Reply("c", 3));
            t2.Result.Should().Be(new Reply("b", 2));
            t1.Result.Should().Be(new Reply("a", 1));
        }

        [Fact]
        public void CorrelatableRef_should_allow_to_cancel_request()
        {
            var source = new CancellationTokenSource();
            var t1 = cref.PostAndAwait<Request, Reply>(target, Req("a", 1), source.Token);
            source.Cancel();
            t1.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public void CorrelatableRef_should_not_allow_to_use_same_correlationId_in_concurrent_requests()
        {
            var t1 = cref.PostAndAwait<Request, Reply>(target, Req("a", 1));
            var t2 = cref.PostAndAwait<Request, Reply>(target, Req("b", 1));

            t1.IsFaulted.Should().BeFalse();
            t2.IsFaulted.Should().BeTrue();
            t2.Exception.InnerExceptions[0].GetType().Should().Be(typeof(CorrelationException));
        }

        [Fact]
        public void CorrelatableRef_should_allow_to_use_same_correlationId_in_non_concurrent_requests()
        {
            var reply1 = cref.PostAndAwait<Request, Reply>(target, Req("a", 1)).Result;
            var reply2 = cref.PostAndAwait<Request, Reply>(target, Req("b", 1)).Result;

            reply1.Should().Be(new Reply("a", 1));
            reply2.Should().Be(new Reply("b", 1));
        }

        [Fact]
        public void CorrelatableRef_should_cancel_all_unfullfiled_request_on_ref_stop()
        {
            var t1 = cref.PostAndAwait<Request, Reply>(target, Req("a", 1));
            var t2 = cref.PostAndAwait<Request, Reply>(target, Req("b", 2));
            Sys.Stop(cref);

            t1.IsCanceled.Should().BeTrue();
            t2.IsCanceled.Should().BeTrue();
        }

        private Request Req(string msg, int correlationId)
        {
            return new Request(msg, correlationId, cref);
        }
    }
}