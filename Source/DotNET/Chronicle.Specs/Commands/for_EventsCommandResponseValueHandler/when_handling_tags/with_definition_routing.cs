// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling_tags;

public class with_definition_routing : given.a_tagged_event_batch
{
    void Establish() => _definition = true;
    async Task Because() => await Handle();

    [Fact] void should_append_both_events() => _appended.Count.ShouldEqual(2);
    [Fact] void should_tag_every_event() => _appended.TrueForAll(_ => _.NamedTags.SequenceEqual(_contextTags)).ShouldBeTrue();
    [Fact] void should_keep_the_definition() => _appended.TrueForAll(_ => _.EventSource == typeof(TestSource)).ShouldBeTrue();
    [Fact] void should_keep_the_stream() => _appended.TrueForAll(_ => _.EventStream == "changes").ShouldBeTrue();
}
