// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_resolving_event_sources;

[Collection("UsesCurrentDirectory")]
public class with_a_declared_aggregate_stream : given.a_host_with_an_event_source
{
    LedgerAggregate _aggregate;

    async Task Because()
    {
        var factory = _scope.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        _aggregate = await factory.Get<LedgerAggregate>(EventSourceId.New());
    }

    [Fact] void should_activate_the_aggregate() => _aggregate.ShouldNotBeNull();
    [Fact] void should_resolve_the_declared_routing() => _mutatorFactory.Received(1).Create<LedgerAggregate>(Arg.Is<IAggregateRootContext>(_ => _.EventSourceType == (EventSourceType)"hosted-ledger" && _.EventStreamType == (EventStreamType)"entries"));
    [Fact] void should_rehydrate_the_aggregate() => _mutator.Received(1).Rehydrate();
}
