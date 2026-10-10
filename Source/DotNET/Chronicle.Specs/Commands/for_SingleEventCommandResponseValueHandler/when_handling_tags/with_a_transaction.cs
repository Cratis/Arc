// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventCommandResponseValueHandler.when_handling_tags;

public class with_a_transaction : given.a_tagged_single_event
{
    void Establish() => _transactional = true;
    async Task Because() => await Handle();

    [Fact] void should_enroll_context_tags() => _enrolled.Single().NamedTags.ShouldEqual(_contextTags);
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_append_immediately() => _appended.ShouldBeEmpty();
    [Fact] void should_use_the_named_tag_overload() => _unitOfWork.ReceivedCalls().Single(_ => _.GetMethodInfo().Name.StartsWith("Add", StringComparison.Ordinal)).GetMethodInfo().Name.ShouldEqual("AddEventWithNamedTags");
}
