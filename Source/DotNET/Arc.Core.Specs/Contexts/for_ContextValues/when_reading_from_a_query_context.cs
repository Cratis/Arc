// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_reading_from_a_query_context : given.a_provided_vessel
{
    object _value;
    Type _performerType;

    void Because()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(int));
        var providers = Substitute.For<IQueryPerformerProviders>();
        providers.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer?>()).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        using var services = new ServiceCollection().AddSingleton(_source).AddSingleton(providers).BuildServiceProvider();
        var context = new QueryContext("Fleet.Vessels", CorrelationId.New(), Paging.NotPaged, Sorting.None, ServiceProvider: services);
        _value = context.Values.Vessel;
        _performerType = _readFor;
    }

    [Fact] void should_read_the_provided_value() => _value.ShouldEqual("Aurora");
    [Fact] void should_read_for_the_part_that_performs_the_query() => _performerType.ShouldEqual(typeof(int));
}
