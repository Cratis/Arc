// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_appending_after_rehydration;

public class with_an_intervening_unhandled_event : given.an_aggregate_rehydrated_from_event_scenario
{
    [EventType]
    record Unhandled;

    AppendResult _interveningResult;
    AppendResult _result;

    async Task Establish() => _interveningResult = await _scenario.EventSequence.Append(_eventSourceId, new Unhandled(), _context.EventStreamType);

    async Task Because() => _result = await AppendFromLoadedAggregate();

    [Fact] void should_append_the_unhandled_event() => _interveningResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_validate_concurrency() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_guard_only_the_handled_event_type() => _scope.EventTypes.ShouldContainOnly(_handledEventType);
}
