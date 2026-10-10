// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling_tags;

public class with_a_subject : given.a_tagged_event_batch
{
    void Establish() => _subject = true;
    async Task Because() => await Handle();

    [Fact] void should_append_both_events() => _appended.Count.ShouldEqual(2);
    [Fact] void should_tag_every_event() => _appended.TrueForAll(_ => _.NamedTags.SequenceEqual(_contextTags)).ShouldBeTrue();
    [Fact] void should_keep_the_subject() => _appended.TrueForAll(_ => _.Subject == new Subject("customer")).ShouldBeTrue();
}
