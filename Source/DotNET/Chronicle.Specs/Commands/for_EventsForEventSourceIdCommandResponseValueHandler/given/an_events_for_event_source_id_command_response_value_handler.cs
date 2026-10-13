// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.given;

public class an_events_for_event_source_id_command_response_value_handler : Specification
{
    protected EventsForEventSourceIdCommandResponseValueHandler _handler;
    protected IEventLog _eventLog;
    protected IEventTypes _eventTypes;
    protected IEventSources _eventSources;
    protected CommandContext _commandContext;
    protected CorrelationId _correlationId;
    protected IConcurrencyScopeStrategies _concurrencyScopeStrategies;
    protected IConcurrencyScopeStrategy _concurrencyScopeStrategy;
    protected EventForEventSourceId[] _batch;
    protected IDictionary<EventSourceId, ConcurrencyScope> _batchScopes;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _eventTypes = Substitute.For<IEventTypes>();
        _eventSources = Substitute.For<IEventSources>();
        _concurrencyScopeStrategies = Substitute.For<IConcurrencyScopeStrategies>();
        _concurrencyScopeStrategy = Substitute.For<IConcurrencyScopeStrategy>();
        _concurrencyScopeStrategies.GetFor(Arg.Any<IEventSequence>()).Returns(_concurrencyScopeStrategy);
        _concurrencyScopeStrategy.StandInForAnOptimisticStrategy();
        _handler = new EventsForEventSourceIdCommandResponseValueHandler(_eventLog, _eventTypes, _concurrencyScopeStrategies, _eventSources);

        _correlationId = Guid.NewGuid();
        var command = new TestCommand();
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], new CommandContextValues(), null);

        _eventLog.AppendMany(default!).ReturnsForAnyArgs(call =>
        {
            _batch = (call.ArgAt<IEnumerable<EventForEventSourceId>?>(0) ?? []).ToArray();
            _batchScopes = call.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3);
            return AppendManyResult.Success(_correlationId, []);
        });

        var successfulResult = AppendResult.Success(_correlationId, EventSequenceNumber.First);
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<ConcurrencyScope>()).Returns(successfulResult);
    }

    public class TestCommand;

    [EventType("Arc.Chronicle.Specs.EventsForEventSourceIdHandler.TestEvent")]
    public record TestEvent(string Name);

    [EventType("Arc.Chronicle.Specs.EventsForEventSourceIdHandler.AnotherTestEvent")]
    public record AnotherTestEvent(int Value);
    public record UnknownEvent(string Data);
}
