// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootServiceCollectionExtensions;

/// <summary>
/// The registration exists to inject a hydrated aggregate root into a command's Handle. Because
/// IAggregateRootFactory.Get is async, the resolved dependency must be the aggregate root itself and not the
/// Task the factory returns — otherwise every command that injects an aggregate root fails to bind its argument.
/// </summary>
public class when_resolving_a_registered_aggregate_root : Specification
{
    ServiceProvider _provider;
    SampleAggregateRoot _aggregateRoot;
    object _resolved;

    void Establish()
    {
        _aggregateRoot = new SampleAggregateRoot();

        var factory = Substitute.For<IAggregateRootFactory>();
        factory.Get<SampleAggregateRoot>(Arg.Any<EventSourceId>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>())
            .Returns(Task.FromResult(_aggregateRoot));

        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(SampleAggregateRoot)]);

        var values = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, EventSourceId.New() }
        };
        var commandContext = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], values, null);

        _provider = new ServiceCollection()
            .AddSingleton(factory)
            .AddScoped(_ => commandContext)
            .AddAggregateRoots(types)
            .BuildServiceProvider();
    }

    void Because()
    {
        using var scope = _provider.CreateScope();
        _resolved = scope.ServiceProvider.GetRequiredService(typeof(SampleAggregateRoot));
    }

    [Fact] void should_resolve_the_aggregate_root_itself() => _resolved.ShouldEqual(_aggregateRoot);
    [Fact] void should_not_resolve_a_task() => (_resolved is Task).ShouldBeFalse();

    void Destroy() => _provider.Dispose();
}
