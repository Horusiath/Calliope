using System;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Calliope.Replication;
using Xunit;
using Xunit.Abstractions;

namespace Calliope.Tests.Replication
{
    public class FailureDetectorTests : TestKit
    {
        private readonly IActorRef failureDetector;

        public FailureDetectorTests(ITestOutputHelper output) : base(output: output)
        {
            failureDetector = Sys.ActorOf(Props.Create(() => new FailureDetector("A", "L1", TimeSpan.FromMilliseconds(500))));
        }

        [Fact]
        public void FailureDetector_should_publish_availability_events_to_ActorSystem_EventStream()
        {
            var probe = CreateTestProbe();

            var cause1 = new Exception("1");
            var cause2 = new Exception("2");

            Sys.EventStream.Subscribe(probe.Ref, typeof(Available));
            Sys.EventStream.Subscribe(probe.Ref, typeof(Unavailable));

            failureDetector.Tell(FailureDetector.AvailabilityDetected.Instance);
            probe.ExpectMsg(new Available("A", "L1"));

            failureDetector.Tell(new FailureDetector.FailureDetected(cause1));
            probe.ExpectMsg<Unavailable>(u => 
                u.EndpointId =="A" 
                && u.JournalId=="L1" 
                && u.Cause.InnerExceptions.Count == 1 
                && u.Cause.InnerExceptions[0] == cause1);

            failureDetector.Tell(FailureDetector.AvailabilityDetected.Instance);
            failureDetector.Tell(FailureDetector.AvailabilityDetected.Instance); // we expect Available only once
            probe.ExpectMsg(new Available("A", "L1"));
            failureDetector.Tell(new FailureDetector.FailureDetected(cause2));

            probe.ExpectMsg<Unavailable>(u =>
                u.EndpointId == "A"
                && u.JournalId == "L1"
                && u.Cause.InnerExceptions.Count == 1
                && u.Cause.InnerExceptions[0] == cause2);

            probe.ExpectMsg<Unavailable>(u =>
                u.EndpointId == "A"
                && u.JournalId == "L1"
                && u.Cause.InnerExceptions.Count == 0);
        }
    }
}