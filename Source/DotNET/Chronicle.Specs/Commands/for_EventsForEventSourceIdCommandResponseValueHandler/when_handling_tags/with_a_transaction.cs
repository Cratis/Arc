// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling_tags;

public class with_a_transaction : given.a_tagged_mixed_batch
{
    void Establish() => _transactional = true;
    async Task Because() => await Handle();

    [Fact] void should_enroll_both_events() => _enrolled.Count.ShouldEqual(2);
    [Fact] void should_merge_wrapper_tags_first_and_remove_duplicates() => _enrolled[0].NamedTags.ShouldEqual([.. _wrapperTags, _contextTags[0]]);
    [Fact] void should_apply_context_tags_to_plain_events() => _enrolled[1].NamedTags.ShouldEqual(_contextTags);
    [Fact] void should_keep_legacy_tags() => _enrolled[0].Tags.ShouldEqual(["legacy"]);
    [Fact] void should_keep_occurrence() => _enrolled[0].Occurred.ShouldEqual(_occurred);
    [Fact] void should_not_append_immediately() => _appended.ShouldBeEmpty();
}
