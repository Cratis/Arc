// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventCommandResponseValueHandler.when_handling_tags;

public class without_a_transaction : given.a_tagged_single_event
{
    async Task Because() => await Handle();

    [Fact] void should_append_context_tags() => _appended.Single().NamedTags.ShouldEqual(_contextTags);
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_enroll_events() => _enrolled.ShouldBeEmpty();
    [Fact] void should_use_the_named_tag_overload() => _eventLog.ReceivedCalls().Single(_ => _.GetMethodInfo().Name.StartsWith("Append", StringComparison.Ordinal)).GetMethodInfo().Name.ShouldEqual("AppendWithNamedTags");
}
