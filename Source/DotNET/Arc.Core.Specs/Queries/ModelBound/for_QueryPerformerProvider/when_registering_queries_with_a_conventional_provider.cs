// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_registering_queries_with_a_conventional_provider : Specification
{
    ServiceProvider _container;
    QueryPerformerProvider _concrete;
    IQueryPerformerProvider _conventional;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(PublicReadModelWithValidQuery)]);
        var services = new ServiceCollection()
            .AddSingleton(types)
            .AddSingleton(new ArtifactAssemblyFilter([], _ => true))
            .AddSingleton<IQueryPerformerProvider, QueryPerformerProvider>();
        services.AddCratisQueries();
        _container = services.BuildServiceProvider();
    }

    void Because()
    {
        _concrete = _container.GetRequiredService<QueryPerformerProvider>();
        _conventional = _container.GetRequiredService<IQueryPerformerProvider>();
    }

    void Destroy() => _container.Dispose();

    [Fact] void should_share_the_filtered_provider() => _conventional.ShouldBeSame(_concrete);
    [Fact] void should_filter_the_concrete_provider() => _concrete.Performers.ShouldBeEmpty();
    [Fact] void should_filter_the_conventional_provider() => _conventional.Performers.ShouldBeEmpty();
}
