// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling_tags;

public class with_a_subject_and_different_static_tags : given.a_tagged_event_batch
{
    void Establish()
    {
        _definition = true;
        _subject = true;
    }

    protected override object CreateValue() => new object[] { new FirstEvent(), new SecondEvent() };

    async Task Because() => await Handle();

    [Fact] void should_keep_the_first_event_type() => _appended[0].Event.ShouldBeOfExactType<FirstEvent>();
    [Fact] void should_keep_the_second_event_type() => _appended[1].Event.ShouldBeOfExactType<SecondEvent>();
    [Fact] void should_leave_static_tags_to_each_event_type() => _eventLog.Received(1).AppendManyWithNamedTags(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<IEnumerable<NamedTag>>(), tags: null, concurrencyScopes: Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
    [Fact] void should_not_apply_the_other_event_type_tags_to_the_first_event() => _appended[0].Tags.ShouldNotContain("second");
    [Fact] void should_not_apply_the_other_event_type_tags_to_the_second_event() => _appended[1].Tags.ShouldNotContain("first");

    [Tag("first")]
    public record FirstEvent;

    [Tag("second")]
    public record SecondEvent;
}
