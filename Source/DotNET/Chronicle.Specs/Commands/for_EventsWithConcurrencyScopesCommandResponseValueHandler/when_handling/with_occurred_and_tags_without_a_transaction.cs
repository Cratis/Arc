// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_occurred_and_tags_without_a_transaction : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    CommandResult _result;
    EventsWithConcurrencyScopes _value;
    DateTimeOffset _occurred;
    string[] _tags;
    EventForEventSourceId[] _appendedEvents;

    void Establish()
    {
        _occurred = new DateTimeOffset(2019, 3, 14, 9, 26, 53, TimeSpan.Zero);
        _tags = ["imported"];
        _value = new([new(EventSourceId.New(), new FirstEvent("first")) { Occurred = _occurred, Tags = _tags }], []);

        _eventLog
            .AppendMany(
                Arg.Any<IEnumerable<EventForEventSourceId>>(),
                Arg.Any<CorrelationId?>(),
                Arg.Any<IEnumerable<string>?>(),
                Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(callInfo =>
            {
                _appendedEvents = callInfo.ArgAt<IEnumerable<EventForEventSourceId>>(0).ToArray();
                return AppendManyResult.Success(_correlationId, [EventSequenceNumber.First]);
            });
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_supplied_occurrence() => _appendedEvents[0].Occurred.ShouldEqual(_occurred);
    [Fact] void should_keep_the_supplied_tags() => _appendedEvents[0].Tags.ShouldContainOnly(_tags);
}
