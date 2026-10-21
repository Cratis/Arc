// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierScopes.when_resolving;

public class with_a_generated_target : Commands.given.a_generated_target
{
    void Because() => _scope = _freshness.Resolve(EventSequenceId.Log, _id, null, false)!;

    [Fact] void should_expect_no_events() => _scope.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_target_the_whole_source() => _scope.EventSourceId.ShouldEqual(_id);
    [Fact] void should_not_narrow_to_a_stream() => _scope.EventStreamType.ShouldBeNull();
    [Fact] void should_not_narrow_to_a_stream_id() => _scope.EventStreamId.ShouldBeNull();
    [Fact] void should_not_narrow_to_a_source_type() => _scope.EventSourceType.ShouldBeNull();
    [Fact] void should_not_narrow_to_event_types() => _scope.EventTypes.ShouldBeNull();
    [Fact] void should_reuse_the_first_guard() => _freshness.Resolve(EventSequenceId.Log, _id, null, false).ShouldBeSame(_scope);
}
