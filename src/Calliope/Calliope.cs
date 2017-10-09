#region copyright
// -----------------------------------------------------------------------
//  <copyright file="Calliope.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using Akka.Actor;

namespace Calliope
{
    public sealed class Calliope : IExtension
    {
        public static Calliope Get(ActorSystem system) => system.WithExtension<Calliope, CalliopeProvider>();
        
        

        public Calliope(ExtendedActorSystem system)
        {
            
        }
    }

    internal sealed class CalliopeProvider : ExtensionIdProvider<Calliope>
    {
        public override Calliope CreateExtension(ExtendedActorSystem system)
        {
            return new Calliope(system);
        }
    }

    public static class CalliopeExtensions
    {
        public static Calliope GetCalliope(this ActorSystem system) => Calliope.Get(system);
    }
}