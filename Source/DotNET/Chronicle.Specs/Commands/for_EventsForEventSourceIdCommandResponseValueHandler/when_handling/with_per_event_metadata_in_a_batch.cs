// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_per_event_metadata_in_a_batch : given.an_events_for_event_source_id_command_response_value_handler
{
    EventForEventSourceId[] _value;
    readonly DateTimeOffset _occurred = new(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);

    void Establish()
    {
        var id = EventSourceId.New();
        _commandContext.Values[WellKnownCommandContextKeys.EventTags] = new NamedTag[] { new("command", "value"), new("shared", "same") };
        _value =
        [
            new(id, new TestEvent("first")) { Subject = new Subject("customer-a"), Occurred = _occurred, Tags = ["imported"], NamedTags = [new("first", "value"), new("shared", "same")] },
            new(id, new TestEvent("second")) { Subject = new Subject("customer-b"), Occurred = _occurred.AddDays(1), Tags = ["credited"], NamedTags = [new("second", "value")] }
        ];
    }

    async Task Because() => await _handler.Handle(_commandContext, _value);

    [Fact] void should_append_as_one_named_tag_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendManyWithNamedTags)).ShouldEqual(1);
    [Fact] void should_keep_subjects_per_event() => _appended.Select(_ => _.Subject).ShouldEqual(_value.Select(_ => _.Subject));
    [Fact] void should_keep_occurrence_per_event() => _appended.Select(_ => _.Occurred).ShouldEqual(_value.Select(_ => _.Occurred));
    [Fact] void should_keep_first_event_tags() => _appended[0].Tags.ShouldEqual(["imported"]);
    [Fact] void should_keep_second_event_tags() => _appended[1].Tags.ShouldEqual(["credited"]);
    [Fact] void should_merge_first_event_named_tags_without_duplicates() => _appended[0].NamedTags.ShouldEqual([new NamedTag("first", "value"), new NamedTag("shared", "same"), new NamedTag("command", "value")]);
    [Fact] void should_merge_second_event_named_tags() => _appended[1].NamedTags.ShouldEqual([new NamedTag("second", "value"), new NamedTag("command", "value"), new NamedTag("shared", "same")]);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append) || _.GetMethodInfo().Name == nameof(_eventLog.AppendWithNamedTags)).ShouldEqual(0);
}
