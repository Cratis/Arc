// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_appending_after_rehydration;

public class without_an_intervening_append : given.an_aggregate_rehydrated_from_event_scenario
{
    AppendResult _result;

    async Task Because() => _result = await AppendFromLoadedAggregate();

    [Fact] void should_succeed_without_a_concurrency_violation() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_validate_concurrency() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
