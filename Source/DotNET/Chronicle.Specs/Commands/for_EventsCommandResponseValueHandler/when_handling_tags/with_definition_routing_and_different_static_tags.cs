// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling_tags;

public class with_definition_routing_and_different_static_tags : given.a_tagged_event_batch
{
    void Establish()
    {
        _definition = true;
        _eventTypes.HasFor(typeof(FirstEvent)).Returns(true);
        _eventTypes.HasFor(typeof(SecondEvent)).Returns(true);
    }

    protected override object CreateValue() => new object[] { new FirstEvent(), new SecondEvent() };

    async Task Because() => await Handle();

    [Fact] void should_append_both_events() => _appended.Count.ShouldEqual(2);
    [Fact] void should_apply_the_distinct_static_tag_union_to_every_event() => _appended.TrueForAll(_ => _.Tags.SequenceEqual(["first", "shared", "second"])).ShouldBeTrue();
    [Fact] void should_keep_the_named_tags_on_every_event() => _appended.TrueForAll(_ => _.NamedTags.SequenceEqual(_contextTags)).ShouldBeTrue();
    [Fact] void should_supply_the_static_tag_union_to_the_batch_append() => _eventLog.Received(1).AppendManyWithNamedTags(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<IEnumerable<NamedTag>>(), tags: Arg.Is<IEnumerable<string>>(_ => _.SequenceEqual(new[] { "first", "shared", "second" })), concurrencyScopes: Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());

    [Tag("first", "shared", "")]
    public record FirstEvent;

    [Tags("second", "shared", " ")]
    public record SecondEvent;
}
