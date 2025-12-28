// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRoot;

public class when_applying_to_aggregate_root : given.an_aggregate_root
{
    FirstEventType _eventToApply;

    void Establish()
    {
        _eventToApply = new(Guid.NewGuid().ToString());
    }

    async Task Because() => await _aggregateRoot.Apply(_eventToApply);

    [Fact] void should_forward_to_mutation() => _mutation.Received(1).Apply(_eventToApply);
}
