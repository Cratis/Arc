// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_occurred_and_tags_in_a_transaction : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    CommandResult _result;
    EventsWithConcurrencyScopes _value;
    DateTimeOffset _occurred;
    string[] _tags;

    void Establish()
    {
        _occurred = new DateTimeOffset(2019, 3, 14, 9, 26, 53, TimeSpan.Zero);
        _tags = ["imported", "batch-7"];
        _value = new(
            [
                new(EventSourceId.New(), new FirstEvent("first")) { Occurred = _occurred, Tags = _tags },
                new(EventSourceId.New(), new SecondEvent(42))
            ],
            []);
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        _result = await _handler.Handle(_commandContext, _value);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_supplied_occurrence() => _unitOfWork.AddedEvents[0].Occurred.ShouldEqual(_occurred);
    [Fact] void should_keep_the_supplied_tags() => _unitOfWork.AddedEvents[0].Tags.ShouldContainOnly(_tags);
    [Fact] void should_leave_an_unset_occurrence_unset() => _unitOfWork.AddedEvents[1].Occurred.ShouldBeNull();
    [Fact] void should_leave_an_untagged_event_untagged() => _unitOfWork.AddedEvents[1].Tags.ShouldBeEmpty();
}
