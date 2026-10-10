// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_handling_tags;

public class with_a_transaction : given.a_tagged_wrapper
{
    void Establish() => _transactional = true;
    async Task Because() => await Handle();

    [Fact] void should_merge_wrapper_tags_first_and_remove_duplicates() => _enrolled.Single().NamedTags.ShouldEqual([.. _wrapperTags, _contextTags[0]]);
    [Fact] void should_keep_legacy_tags() => _enrolled.Single().Tags.ShouldEqual(["legacy"]);
    [Fact] void should_keep_occurrence() => _enrolled.Single().Occurred.ShouldEqual(_occurred);
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_append_immediately() => _appended.ShouldBeEmpty();
}
