// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling_tags;

public class with_definition_routing : given.a_tagged_mixed_batch
{
    void Establish() => _definition = true;
    async Task Because() => await Handle();

    [Fact] void should_append_both_events() => _appended.Count.ShouldEqual(2);
    [Fact] void should_merge_wrapper_tags_first() => _appended[0].NamedTags.ShouldEqual([.. _wrapperTags, _contextTags[0]]);
    [Fact] void should_tag_plain_events() => _appended[1].NamedTags.ShouldEqual(_contextTags);
    [Fact] void should_keep_the_definition() => _appended.TrueForAll(_ => _.EventSource == typeof(TestSource)).ShouldBeTrue();
    [Fact] void should_keep_the_stream() => _appended.TrueForAll(_ => _.EventStream == "changes").ShouldBeTrue();
    [Fact] void should_keep_legacy_tags() => _appended[0].Tags.ShouldEqual(["legacy"]);
    [Fact] void should_keep_occurrence() => _appended[0].Occurred.ShouldEqual(_occurred);
    [Fact] void should_not_enroll_events() => _enrolled.ShouldBeEmpty();
}
