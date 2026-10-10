// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_appending_after_rehydration;

public class with_an_intervening_append_without_routing : given.an_aggregate_rehydrated_from_event_scenario
{
    AppendResult _result;

    async Task Because()
    {
        await _scenario.EventSequence.Append(_eventSourceId, new Changed());
        _result = await AppendFromLoadedAggregate();
    }

    [Fact] void should_reject_the_stale_aggregate() => _result.HasConcurrencyViolations.ShouldBeTrue();
}
