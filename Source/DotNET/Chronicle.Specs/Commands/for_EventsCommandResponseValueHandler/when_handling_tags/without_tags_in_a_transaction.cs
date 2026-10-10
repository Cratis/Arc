// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling_tags;

public class without_tags_in_a_transaction : given.a_tagged_event_batch
{
    void Establish()
    {
        _transactional = true;
        _tagged = false;
    }
    async Task Because() => await Handle();

    [Fact] void should_enroll_both_events() => _enrolled.Count.ShouldEqual(2);
    [Fact] void should_keep_the_old_enrollment_call() => _unitOfWork.ReceivedCalls().Where(_ => _.GetMethodInfo().Name.StartsWith("Add", StringComparison.Ordinal)).All(_ => _.GetMethodInfo().Name == "AddEvent").ShouldBeTrue();
    [Fact] void should_not_append_immediately() => _appended.ShouldBeEmpty();
}
