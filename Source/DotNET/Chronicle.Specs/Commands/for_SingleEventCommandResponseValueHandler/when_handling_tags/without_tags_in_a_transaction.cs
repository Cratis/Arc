// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventCommandResponseValueHandler.when_handling_tags;

public class without_tags_in_a_transaction : given.a_tagged_single_event
{
    void Establish()
    {
        _transactional = true;
        _tagged = false;
    }
    async Task Because() => await Handle();

    [Fact] void should_enroll_without_tags() => _enrolled.Single().NamedTags.ShouldBeEmpty();
    [Fact] void should_keep_the_old_enrollment_call() => _unitOfWork.ReceivedCalls().Single(_ => _.GetMethodInfo().Name.StartsWith("Add", StringComparison.Ordinal)).GetMethodInfo().Name.ShouldEqual("AddEvent");
    [Fact] void should_not_append_immediately() => _appended.ShouldBeEmpty();
}
