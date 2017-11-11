// #region copyright
// -----------------------------------------------------------------------
//  <copyright file="ReplicatorStage.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
// #endregion

using Akka.Streams;
using Akka.Streams.Stage;

namespace Calliope.Replication
{
    sealed class ReplicatorStage<T> : GraphStage<FlowShape<T, Versioned<T>>>
    {
        #region logic

        private sealed class Logic : GraphStageLogic
        {
            private readonly ReplicatorStage<T> stage;

            public Logic(ReplicatorStage<T> stage) : base(stage.Shape)
            {
                this.stage = stage;
            }
        }

        #endregion

        public ReplicatorStage()
        {
            Shape = new FlowShape<T, Versioned<T>>(In, Out);
        }

        public Inlet<T> In { get; } = new Inlet<T>("replicator.in");
        public Outlet<Versioned<T>> Out { get; } = new Outlet<Versioned<T>>("replicator.out");
        public override FlowShape<T, Versioned<T>> Shape { get; }
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);
    }
}