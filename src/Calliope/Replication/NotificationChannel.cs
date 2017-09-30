#region copyright
// -----------------------------------------------------------------------
//  <copyright file="NotificationChannel.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using Akka;
using Akka.Streams;
using Akka.Streams.Stage;

namespace Calliope.Replication
{
    /// <summary>
    /// A graph stage responsible for reliable at least once delivery.
    /// </summary>
    public sealed class NotificationChannel<T> : GraphStage<FlowShape<IReplicationRequest<T>, IReplicationResponse>>
    {
        private readonly string channelId;

        public NotificationChannel(string channelId)
        {
            this.channelId = channelId;
            Shape = new FlowShape<IReplicationRequest<T>, IReplicationResponse>(Inlet, Outlet);
        }

        public Inlet<IReplicationRequest<T>> Inlet { get; } = new Inlet<IReplicationRequest<T>>("notification.in");
        public Outlet<IReplicationResponse> Outlet { get; } = new Outlet<IReplicationResponse>("notification.out");

        public override FlowShape<IReplicationRequest<T>, IReplicationResponse> Shape { get; }

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        #region logic

        private sealed class Logic : GraphStageLogic
        {
            public Logic(NotificationChannel<T> stage) : base(stage.Shape)
            {
                SetHandler(stage.Inlet, onPush: () =>
                {
                    
                }, onUpstreamFinish: () =>
                {

                }, onUpstreamFailure: error =>
                {
                    
                });

                SetHandler(stage.Outlet, onPull: () =>
                {
                    
                }, onDownstreamFinish: () =>
                {
                    
                });
            }
        }

        #endregion
    }
}