// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

public class an_invalid_event : given.an_aggregate_mutation_with_event_registry
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(() => _mutation.Apply(new object()));

    [Fact] void should_reject_the_unregistered_event() => _result.ShouldBeOfExactType<TypeIsNotAnEventType>();
}
