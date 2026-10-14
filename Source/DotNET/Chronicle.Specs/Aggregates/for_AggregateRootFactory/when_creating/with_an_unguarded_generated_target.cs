// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory.when_creating;

public class with_an_unguarded_generated_target : given.a_generated_aggregate_target
{
    async Task Establish()
    {
        await Capture<CreatesUnguardedId>();
        _sequence.HasEventsFor(_id).Returns(true);
    }

    async Task Because() => _aggregate = await InInvocation(() => _factory.Get<NewAggregate>(_id));

    [Fact] void should_not_check_freshness() => _sequence.DidNotReceive().HasEventsFor(_id);
    [Fact] void should_preserve_activation() => _aggregate.Activated.ShouldBeTrue();
}
