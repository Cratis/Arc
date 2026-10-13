// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling_tags;

public class with_definition_routing_and_a_subject : given.a_tagged_event_batch
{
    void Establish()
    {
        _definition = true;
        _subject = true;
    }

    async Task Because() => await Handle();

    [Fact] void should_append_as_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendManyWithNamedTags)).ShouldEqual(1);
    [Fact] void should_append_both_events() => _appended.Count.ShouldEqual(2);
    [Fact] void should_keep_the_subject() => _appended.TrueForAll(_ => _.Subject == new Subject("customer")).ShouldBeTrue();
    [Fact] void should_keep_the_definition() => _appended.TrueForAll(_ => _.EventSource == typeof(TestSource) && _.EventStream == "changes").ShouldBeTrue();
    [Fact] void should_keep_the_named_tags() => _appended.TrueForAll(_ => _.NamedTags.SequenceEqual(_contextTags)).ShouldBeTrue();
}
