// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_appending_after_rehydration;

public class with_an_unhandled_event_at_the_tail : given.an_aggregate_rehydrated_from_event_scenario
{
    [EventType("a2625cbe-27ef-48b1-8ec5-b55e5d7c9618")]
    record Unhandled;

    AppendResult _result;

    protected override async Task<IEventSequence> GetRehydrationEventSequence()
    {
        await _scenario.EventSequence.Append(_eventSourceId, new Unhandled(), new TestAggregateRoot().GetEventStreamType());
        return _scenario.EventSequence;
    }

    async Task Because() => _result = await AppendFromLoadedAggregate();

    [Fact] void should_succeed_without_a_competing_writer() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_report_a_concurrency_violation() => _result.HasConcurrencyViolations.ShouldBeFalse();
}
