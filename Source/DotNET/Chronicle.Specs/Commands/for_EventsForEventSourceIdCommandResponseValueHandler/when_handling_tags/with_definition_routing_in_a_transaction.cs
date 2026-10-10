// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling_tags;

public class with_definition_routing_in_a_transaction : given.a_tagged_mixed_batch
{
    void Establish()
    {
        _transactional = true;
        _definition = true;
    }
    async Task Because() => await Handle();

    [Fact] void should_enroll_both_events() => _enrolled.Count.ShouldEqual(2);
    [Fact] void should_merge_wrapper_tags_first() => _enrolled[0].NamedTags.ShouldEqual([.. _wrapperTags, _contextTags[0]]);
    [Fact] void should_tag_plain_events() => _enrolled[1].NamedTags.ShouldEqual(_contextTags);
    [Fact] void should_keep_the_definition() => _enrolled.TrueForAll(_ => _.EventSource == typeof(TestSource)).ShouldBeTrue();
    [Fact] void should_keep_the_stream() => _enrolled.TrueForAll(_ => _.EventStream == "changes").ShouldBeTrue();
    [Fact] void should_not_append_immediately() => _appended.ShouldBeEmpty();
}
