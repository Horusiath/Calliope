using System;
using Akka.Actor;
using Akka.Configuration;

namespace Lighthouse
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ConfigurationFactory.ParseString(@"
                akka.actor {
                    provider = cluster
                    serializers {
                        hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                    }
                    serialization-bindings {
                        ""System.Object"" = hyperion
                    }
                }
                akka.remote.dot-netty.tcp {
                    port = 2552
                    hostname = ""localhost""
                }
                akka.cluster.seed-nodes = [ ""akka.tcp://calliope@localhost:2552/"" ]");
            using (var system = ActorSystem.Create("calliope", config))
            {
                Console.ReadLine();
            }
        }
    }
}
