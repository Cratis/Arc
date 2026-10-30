// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootServiceCollectionExtensions.given;

public abstract class an_aggregate_root_factory_that_fails : Specification
{
    protected ServiceProvider _provider;
    protected InvalidOperationException _failure;
    protected Exception _exception;

    void Establish()
    {
        _failure = new InvalidOperationException("factory failure");

        var factory = Substitute.For<IAggregateRootFactory>();
        ConfigureFactory(factory);

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

    protected void ResolveAggregateRoot() => _exception = Catch.Exception(() =>
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService(typeof(SampleAggregateRoot));
    });

    void Destroy() => _provider.Dispose();

    protected abstract void ConfigureFactory(IAggregateRootFactory factory);
}
