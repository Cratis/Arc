// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling_tags;

public class with_a_transaction : given.a_tagged_event_batch
{
    void Establish() => _transactional = true;
    async Task Because() => await Handle();

    [Fact] void should_enroll_both_events() => _enrolled.Count.ShouldEqual(2);
    [Fact] void should_tag_every_event() => _enrolled.TrueForAll(_ => _.NamedTags.SequenceEqual(_contextTags)).ShouldBeTrue();
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_append_immediately() => _appended.ShouldBeEmpty();
    [Fact] void should_use_the_named_tag_overload() => _unitOfWork.ReceivedCalls().Where(_ => _.GetMethodInfo().Name.StartsWith("Add", StringComparison.Ordinal)).All(_ => _.GetMethodInfo().Name == "AddEventWithNamedTags").ShouldBeTrue();
}
