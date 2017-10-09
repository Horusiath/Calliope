using Akka.Actor;
using Akka.Configuration;
using Calliope;

namespace CalliopeSamples
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ConfigurationFactory.ParseString(@"
                akka.actor.provider = cluster
                akka.remote.dot-netty.tcp {
                    port = 0
                    hostname = ""localhost""
                }
                akka.cluster.seeds = [ ""akka.tcp://calliope@localhost:2552/"" ]");
            using (var system = ActorSystem.Create("calliope"))
            {
                var calliope = system.GetCalliope();
            }
        }
    }
}
