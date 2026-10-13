// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling;

public class and_the_second_subject_event_is_refused : given.an_events_command_response_value_handler
{
    object[] _events;
    readonly List<object> _persisted = [];
    CommandResult _result;

    void Establish()
    {
        _events = [new TestEvent("First"), new AnotherTestEvent(2)];
        _commandContext.Values[WellKnownCommandContextKeys.Subject] = new Subject("customer");
        var violation = new ConstraintViolation(EventTypeId.Unknown, EventSequenceNumber.Unavailable, ConstraintType.Unknown, new ConstraintName("SecondEvent"), new ConstraintViolationMessage("Second event refused"), new ConstraintViolationDetails());
        _eventLog.Append(default!, default!).ReturnsForAnyArgs(call =>
        {
            var @event = call.ArgAt<object>(1);
            if (@event == _events[1])
            {
                return AppendResult.Failed(_correlationId, [violation]);
            }
            _persisted.Add(@event);
            return AppendResult.Success(_correlationId, EventSequenceNumber.First);
        });
        _eventLog.AppendMany(default(EventSourceId)!, default!).ReturnsForAnyArgs(call =>
        {
            var events = call.ArgAt<IEnumerable<object>>(1).ToArray();
            if (events.Contains(_events[1]))
            {
                return AppendManyResult.Failed(_correlationId, [violation]);
            }
            _persisted.AddRange(events);
            return AppendManyResult.Success(_correlationId, []);
        });
        _eventLog.AppendMany(default!).ReturnsForAnyArgs(call =>
        {
            var events = call.ArgAt<IEnumerable<EventForEventSourceId>>(0).Select(_ => _.Event).ToArray();
            if (events.Contains(_events[1]))
            {
                return AppendManyResult.Failed(_correlationId, [violation]);
            }
            _persisted.AddRange(events);
            return AppendManyResult.Success(_correlationId, []);
        });
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_append_nothing() => _persisted.ShouldBeEmpty();
    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("Second event refused");
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
