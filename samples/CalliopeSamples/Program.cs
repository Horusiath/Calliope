using System;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Calliope.Replication;

namespace CalliopeSamples
{
    class Printer : ReceiveActor
    {
        public Printer()
        {
            Receive<Versioned<string>>(ver =>
            {
                Console.WriteLine($"Received: {ver}");
            });
        }
    }

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
                    port = 0
                    hostname = ""localhost""
                }
                akka.cluster {
                    roles = [ ""calliope"" ]
                    seed-nodes = [ ""akka.tcp://calliope@localhost:2552/"" ]
                }");
            using (var system = ActorSystem.Create("calliope", config))
            {
                var calliope = Calliope.Calliope.Get(system);
                var topic = calliope.TopicRef<string>("test");
                var printer = system.ActorOf(Props.Create<Printer>(), "printer");
                topic.Tell(new Subscribe<string>(printer));

                string str;
                do
                {
                    Console.Write("> ");
                    str = Console.ReadLine();

                    topic.Tell(new Broadcast<string>(str));

                } while (str != "q");
            }
        }
    }
}
