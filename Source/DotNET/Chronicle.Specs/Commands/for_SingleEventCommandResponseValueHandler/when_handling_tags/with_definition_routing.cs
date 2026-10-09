// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventCommandResponseValueHandler.when_handling_tags;

public class with_definition_routing : given.a_tagged_single_event
{
    void Establish() => _definition = true;
    async Task Because() => await Handle();

    [Fact] void should_append_context_tags() => _appended.Single().NamedTags.ShouldEqual(_contextTags);
    [Fact] void should_keep_the_definition() => _appended.Single().EventSource.ShouldEqual(typeof(TestSource));
    [Fact] void should_keep_the_stream() => _appended.Single().EventStream.ShouldEqual("changes");
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
}
