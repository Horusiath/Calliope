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
                akka.actor.provider = cluster
                akka.remote.dot-netty.tcp {
                    port = 2552
                    hostname = ""localhost""
                }");
            using (var system = ActorSystem.Create("calliope", config))
            {
                Console.ReadLine();
            }
        }
    }
}
