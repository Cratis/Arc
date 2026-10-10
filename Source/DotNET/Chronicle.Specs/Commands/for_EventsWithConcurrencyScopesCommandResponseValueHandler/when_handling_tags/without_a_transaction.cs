// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling_tags;

public class without_a_transaction : given.a_tagged_scoped_batch
{
    async Task Because() => await Handle();

    [Fact] void should_merge_wrapper_tags_first_and_remove_duplicates() => _appended.Single().NamedTags.ShouldEqual([.. _wrapperTags, _contextTags[0]]);
    [Fact] void should_keep_legacy_tags() => _appended.Single().Tags.ShouldEqual(["legacy"]);
    [Fact] void should_keep_occurrence() => _appended.Single().Occurred.ShouldEqual(_occurred);
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_named_tag_batch_overload() => _eventLog.ReceivedCalls().Single(_ => _.GetMethodInfo().Name.StartsWith("Append", StringComparison.Ordinal)).GetMethodInfo().Name.ShouldEqual("AppendManyWithNamedTags");
}
