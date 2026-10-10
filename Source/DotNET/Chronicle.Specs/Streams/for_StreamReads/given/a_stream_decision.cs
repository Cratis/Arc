// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.given;

public class a_stream_decision : Specification
{
    protected CommandScenario<DecideStream> _scenario;
    protected EventSourceId _id;
    protected EventRoute _route;
    protected CommandResult _result;

    void Establish()
    {
        _id = EventSourceId.New();
        _route = new("reports", "approval", "owner:month");
        _scenario = new CommandScenario<DecideStream>().UseDecisionReads();
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command]
    [EventSourceType("reports")]
    [EventStreamType("approval")]
    [EventStreamId("{Owner}:{Month}")]
    public record DecideStream(EventSourceId EventSourceId, string Owner = "owner", string Month = "month", bool WholeType = false, bool Retry = false)
    {
        public Task<StreamDecision> Provide(IStreamReads reads, ICommandContextAccessor context) => WholeType
            ? reads.StreamType(EventSourceId, context.Current.GetEventRoute())
            : reads.ForCommand();

        public EventsWithConcurrencyScopes Handle(StreamDecision decision) => Retry
            ? decision.Append([])
            : decision.Append(new Approved(decision.Events.Count, decision.IsEmpty));
    }

    [Command]
    [EventSource<ReportSource>("approval")]
    [EventStreamId("{Owner}:{Month}")]
    public record DecideDefinition(EventSourceId EventSourceId, OwnerId Owner, string Month)
    {
        public Task<StreamDecision> Provide(IStreamReads reads) => reads.ForCommand();
        public EventsWithConcurrencyScopes Handle(StreamDecision decision) => decision.Append(new Approved(decision.Events.Count, decision.IsEmpty));
    }

    [Command]
    [EventSource<ReportSource>("approval")]
    [EventStreamId("{Owner}:{Month}")]
    public record PlainAndRouted(EventSourceId EventSourceId, OwnerId Owner, string Month)
    {
        public (Approved Plain, EventForEventSourceId Routed) Handle(ICommandContextAccessor context) =>
            (new(0, true), context.Current.GetEventRoute().Route(EventSourceId, new PriorFact("routed")));
    }

    public record OwnerId(string Value) : ConceptAs<string>(Value);

    [Cratis.Chronicle.EventSources.EventSource("reports", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType)]
    [EventStream("approval", Concurrency = ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
    public class ReportSource : IEventSource;

    [EventType("1e64c963-b3b7-4eea-a9b5-dac14e6bca21")]
    public record Approved(int PriorCount, bool WasEmpty);

    [EventType("46b4ae5e-2fb0-4d6c-bef5-e1d199b260f9")]
    public record PriorFact(string Value);
}
