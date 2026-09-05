// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

/// <summary>
/// An expected tail belongs to one stream, so a command writing across streams needs a scope per target.
/// Sharing one scope would hand every other stream the first stream's expected tail, which is wrong for each
/// of them — it would reject appends that do not conflict and pass ones that do.
/// </summary>
public class and_the_command_is_scoped_for_concurrency : given.an_events_for_event_source_id_command_response_value_handler
{
    object[] _value;
    CommandResult _result;
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;

    void Establish()
    {
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();

        var command = new ConcurrencyScopedCommand();
        _commandContext = new CommandContext(_correlationId, typeof(ConcurrencyScopedCommand), command, [], new CommandContextValues(), null);

        _value =
        [
            new EventForEventSourceId(_firstEventSourceId, new TestEvent("First")),
            new EventForEventSourceId(_secondEventSourceId, new AnotherTestEvent(2)),
            new EventForEventSourceId(_firstEventSourceId, new TestEvent("Third"))
        ];
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_resolve_a_scope_for_the_first_event_source() =>
        _concurrencyScopeStrategy.Received(1).GetScope(_firstEventSourceId, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>());

    [Fact] void should_resolve_a_scope_for_the_second_event_source() =>
        _concurrencyScopeStrategy.Received(1).GetScope(_secondEventSourceId, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>());

    [Fact] void should_append_to_the_first_event_source_with_its_own_scope() => _eventLog.Received(2).Append(
        _firstEventSourceId,
        Arg.Any<object>(),
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Is<ConcurrencyScope>(scope => scope != null && scope.EventSourceId == _firstEventSourceId));

    [Fact] void should_append_to_the_second_event_source_with_its_own_scope() => _eventLog.Received(1).Append(
        _secondEventSourceId,
        Arg.Any<object>(),
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Is<ConcurrencyScope>(scope => scope != null && scope.EventSourceId == _secondEventSourceId));

    [EventSourceType("Thing", concurrency: true)]
    class ConcurrencyScopedCommand;
}
