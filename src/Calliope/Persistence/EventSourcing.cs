#region copyright
// -----------------------------------------------------------------------
//  <copyright file="EventSourcing.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;

namespace Calliope.Persistence
{
    public static class EventSourcing
    {
        /// <summary>
        /// Creates a router that routes to different <paramref name="processor"/>s based on input element key <typeparamref name="TKey"/>.
        /// A key is computed from input elements with the <paramref name="keySelector"/> function. Whenever a new key is
        /// encountered, a new key-specific processor is created with the <paramref name="processor"/> function.
        /// A processor processes all input elements of given key. Processor output elements
        /// are merged back into the main flow returned by this method.
        /// </summary>
        /// <param name="keySelector">computes a key from an input element</param>
        /// <param name="processor">key-specific processor factory</param>
        /// <param name="maxProcessors">maximum numbers of concurrent processors</param>
        /// <typeparam name="TIn">router and processor input type</typeparam>
        /// <typeparam name="TOut">router and processor output type</typeparam>
        /// <typeparam name="TKey">key type</typeparam>
        /// <typeparam name="TMat">processor materialized value type</typeparam>
        public static Flow<TIn, TOut, NotUsed> Router<TIn, TOut, TKey, TMat>(
            Func<TIn, TKey> keySelector,
            Func<TKey, Flow<TIn, TOut, TMat>> processor,
            int maxProcessors = int.MaxValue) =>
            (Flow<TIn, TOut, NotUsed>)Flow.Create<TIn>()
                .GroupBy(maxProcessors, keySelector)
                .PrefixAndTail(1)
                .MergeMany(maxProcessors, tuple =>
                {
                    (var h, var t) = tuple;
                    return Source.From(h).Concat(t).Via(processor(keySelector(h.First())));
                })
                .MergeSubstreams();

        /// <summary>
        /// Creates a bidi-flow that implements the driver for event sourcing logic defined by `requestHandler`
        /// `eventHandler`. The created event sourcing stage should be joined with an event log (i.e. a flow)
        /// for writing emitted events. Written events are delivered from the joined event log back to the stage:
        /// 
        ///  - After materialization, the stage's state is recovered with replayed events delivered by the joined
        ///    event log.
        ///  - On recovery completion (see [[Delivery]]) the stage is ready to accept requests if there is
        ///    downstream response and event demand.
        ///  - On receiving a command it calls the request handler and emits the returned events. The emitted events
        ///    are sent downstream to the joined event log.
        ///  - For each written event that is delivered from the event log back to the event sourcing stage, the
        ///    event handler is called with that event and the current state. The stage updates its current state
        ///    with the event handler result.
        ///  - After all emitted events (for a given command) have been applied, the response function, previously
        ///    created by the command handler, is called with the current state and the created response is emitted.
        ///  - After response emission, the stage is ready to accept the next request if there is downstream response
        ///    and event demand.
        /// </summary>
        /// <param name="emitterId">Identifier used for <see cref="Emitted{T}.EmitterId"/>.</param> 
        /// <param name="initialState">Initial state</param>
        /// <param name="requestHandler">The stage's request handler</param>
        /// <param name="eventHandler">The stage's event handler.</param>
        /// <typeparam name="TState">State type</typeparam>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <typeparam name="TRequest">Request type.</typeparam>
        /// <typeparam name="TResponse">Response type.</typeparam>
        public static BidiFlow<TRequest, Emitted<TEvent>, Delivery<Durable<TEvent>>, TResponse, NotUsed> Create<TState, TEvent, TRequest, TResponse>(
            string emitterId,
            TState initialState,
            RequestHandler<TState, TEvent, TRequest, TResponse> requestHandler,
            EventHandler<TState, TEvent> eventHandler)
        {
            return BidiFlow.FromGraph(new EventSourcing<TState, TEvent, TRequest, TResponse>(emitterId, initialState, _ => requestHandler, _ => eventHandler));
        }

        /// <summary>
        /// Creates a bidi-flow that implements the driver for event sourcing logic returned by `requestHandlerProvider`
        /// and `eventHandlerProvider`. `requestHandlerProvider` is evaluated with current state for each received request,
        /// `eventHandlerProvider` is evaluated with current state for each written event. This can be used by applications
        /// to switch request and event handling logic as a function of current state. The created event sourcing stage
        /// should be joined with an event log (i.e. a flow) for writing emitted events. Written events are delivered
        /// from the joined event log back to the stage:
        /// 
        ///  - After materialization, the stage's state is recovered with replayed events delivered by the joined
        ///    event log.
        ///  - On recovery completion (see [[Delivery]]) the stage is ready to accept requests if there is
        ///    downstream response and event demand.
        ///  - On receiving a command it calls the request handler and emits the returned events. The emitted events
        ///    are sent downstream to the joined event log.
        ///  - For each written event that is delivered from the event log back to the event sourcing stage, the
        ///    event handler is called with that event and the current state. The stage updates its current state
        ///    with the event handler result.
        ///  - After all emitted events (for a given command) have been applied, the response function, previously
        ///    created by the command handler, is called with the current state and the created response is emitted.
        ///  - After response emission, the stage is ready to accept the next request if there is downstream response
        ///    and event demand.
        /// </summary>
        /// <param name="emitterId">Identifier used for <see cref="Emitted{T}.EmitterId"/>.</param> 
        /// <param name="initialState">Initial state</param>
        /// <param name="requestHandlerProvider">Provide used to generate stage's request handler for a given state.</param>
        /// <param name="eventHandlerProvider">Provider used to generate stage's event handler for a given state.</param>
        /// <typeparam name="TState">State type</typeparam>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <typeparam name="TRequest">Request type.</typeparam>
        /// <typeparam name="TResponse">Response type.</typeparam>
        public static BidiFlow<TRequest, Emitted<TEvent>, Delivery<Durable<TEvent>>, TResponse, NotUsed> Create<TState, TEvent, TRequest, TResponse>(
            string emitterId,
            TState initialState,
            Func<TState, RequestHandler<TState, TEvent, TRequest, TResponse>> requestHandlerProvider,
            Func<TState, EventHandler<TState, TEvent>> eventHandlerProvider)
        {
            return BidiFlow.FromGraph(new EventSourcing<TState, TEvent, TRequest, TResponse>(emitterId, initialState, requestHandlerProvider, eventHandlerProvider));
        }
    }

    internal sealed class EventSourcing<TState, TEvent, TRequest, TResponse> : GraphStage<BidiShape<TRequest, Emitted<TEvent>, Delivery<Durable<TEvent>>, TResponse>>
    {
        private readonly string emitterId;
        private readonly TState initialState;
        private readonly Func<TState, RequestHandler<TState, TEvent, TRequest, TResponse>> requestHandlerProvider;
        private readonly Func<TState, EventHandler<TState, TEvent>> eventHanlderProvider;

        public EventSourcing(string emitterId, TState initialState, Func<TState, RequestHandler<TState, TEvent, TRequest, TResponse>> requestHandlerProvider, Func<TState, EventHandler<TState, TEvent>> eventHanlderProvider)
        {
            this.emitterId = emitterId;
            this.initialState = initialState;
            this.requestHandlerProvider = requestHandlerProvider;
            this.eventHanlderProvider = eventHanlderProvider;

            Shape = new BidiShape<TRequest, Emitted<TEvent>, Delivery<Durable<TEvent>>, TResponse>(RequestIn, EventOut, EventIn, ResponseOut);
        }

        public Inlet<TRequest> RequestIn { get; } = new Inlet<TRequest>("EventSourcing.requestIn");
        public Outlet<Emitted<TEvent>> EventOut { get; } = new Outlet<Emitted<TEvent>>("EventSourcing.eventOut");
        public Inlet<Delivery<Durable<TEvent>>> EventIn { get; } = new Inlet<Delivery<Durable<TEvent>>>("EventSourcing.eventIn");
        public Outlet<TResponse> ResponseOut { get; } = new Outlet<TResponse>("EventSourcing.responseOut");
        public override BidiShape<TRequest, Emitted<TEvent>, Delivery<Durable<TEvent>>, TResponse> Shape { get; }
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        #region internal classes

        private struct Roundtrip
        {
            public ImmutableHashSet<Guid> EmissionIds { get; }
            public Func<TState, TResponse> ResponseFactory { get; }

            public Roundtrip(ImmutableHashSet<Guid> emissionIds, Func<TState, TResponse> responseFactory)
            {
                this.EmissionIds = emissionIds;
                this.ResponseFactory = responseFactory;
            }

            public Roundtrip Delivered(Guid emissionId) => new Roundtrip(EmissionIds.Remove(emissionId), ResponseFactory);
        }

        private sealed class Logic : GraphStageLogic
        {
            private EventSourcing<TState, TEvent, TRequest, TResponse> stage;

            private bool requestUpstreamFinished = false;
            private bool recovered = false;
            private Roundtrip? roundtrip = null;
            private TState state;

            public Logic(EventSourcing<TState, TEvent, TRequest, TResponse> stage) : base(stage.Shape)
            {
                this.stage = stage;
                this.state = stage.initialState;

                SetHandler(stage.RequestIn, onPush: () =>
                {
                    var emission = stage.requestHandlerProvider(state)(state, Grab(stage.RequestIn));
                    switch (emission)
                    {
                        case Respond<TState, TEvent, TResponse> respond:
                            Push(stage.ResponseOut, respond.Response);
                            TryPullRequestIn();
                            break;
                        case Emit<TState, TEvent, TResponse> emit:
                            var emitted = emit.Events.Select(e => new Emitted<TEvent>(e, stage.emitterId));
                            roundtrip = new Roundtrip(emitted.Select(e => e.EmissionId).ToImmutableHashSet(), emit.ResponseFactory);
                            break;
                    }
                }, onUpstreamFinish: () =>
                {
                    if (!roundtrip.HasValue) CompleteStage();
                    else requestUpstreamFinished = true;
                });

                SetHandler(stage.EventIn, onPush: () =>
                {
                    switch (Grab(stage.EventIn))
                    {
                        case Delivered<Durable<TEvent>> delivered:
                            state = stage.eventHanlderProvider(state)(state, delivered.Data.Event);
                            if (roundtrip.HasValue)
                            {
                                var r = roundtrip.Value.Delivered(delivered.Data.EmissionId);
                                if (r.EmissionIds.IsEmpty)
                                {
                                    Push(stage.ResponseOut, r.ResponseFactory(state));

                                    if (requestUpstreamFinished) CompleteStage();
                                    else TryPullRequestIn();

                                    roundtrip = null;
                                }
                                else
                                {
                                    roundtrip = r;
                                }
                            }
                            break;
                        case Recovered<Durable<TEvent>> _:
                            recovered = true;
                            TryPullRequestIn();
                            break;
                    }
                });

                SetHandler(stage.EventOut, onPull: TryPullRequestIn);

                SetHandler(stage.ResponseOut, onPull: TryPullRequestIn);
            }

            public override void PreStart()
            {
                base.PreStart();
                TryPullEventIn();
            }

            private void TryPullEventIn()
            {
                if (!requestUpstreamFinished) Pull(stage.EventIn);
            }

            private void TryPullRequestIn()
            {
                if (!requestUpstreamFinished && recovered && roundtrip == null && IsAvailable(stage.ResponseOut) && !HasBeenPulled(stage.RequestIn))
                    Pull(stage.RequestIn);
            }
        }

        #endregion
    }
}