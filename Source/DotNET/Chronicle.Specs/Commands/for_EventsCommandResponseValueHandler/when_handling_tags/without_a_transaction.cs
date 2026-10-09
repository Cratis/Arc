// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling_tags;

public class without_a_transaction : given.a_tagged_event_batch
{
    async Task Because() => await Handle();

    [Fact] void should_append_both_events() => _appended.Count.ShouldEqual(2);
    [Fact] void should_tag_every_event() => _appended.TrueForAll(_ => _.NamedTags.SequenceEqual(_contextTags)).ShouldBeTrue();
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_enroll_events() => _enrolled.ShouldBeEmpty();
    [Fact] void should_use_the_named_tag_batch_overload() => _eventLog.ReceivedCalls().Single(_ => _.GetMethodInfo().Name.StartsWith("Append", StringComparison.Ordinal)).GetMethodInfo().Name.ShouldEqual("AppendManyWithNamedTags");
}
