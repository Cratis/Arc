// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling_tags;

public class with_definition_routing : given.a_tagged_scoped_batch
{
    void Establish() => _definition = true;
    async Task Because() => await Handle();

    [Fact] void should_merge_wrapper_tags_first() => _appended.Single().NamedTags.ShouldEqual([.. _wrapperTags, _contextTags[0]]);
    [Fact] void should_keep_the_definition() => _appended.Single().EventSource.ShouldEqual(typeof(TestSource));
    [Fact] void should_keep_the_stream() => _appended.Single().EventStream.ShouldEqual("changes");
}
