// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Neo4j.for_ServiceCollectionExtensions;

public class when_adding_cratis_neo4j_with_a_custom_resolver : Specification
{
    ServiceProvider _provider;
    IGraphDatabaseNameResolver _resolver;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ITenantIdAccessor>());
        services.AddCratisNeo4j(configureNeo4j: neo4j => neo4j.WithDatabaseNameResolver<FixedResolver>(), neo4jConfigSectionPath: string.Empty);
        _provider = services.BuildServiceProvider();
    }

    void Because() => _resolver = _provider.GetRequiredService<IGraphDatabaseNameResolver>();

    [Fact] void should_use_the_custom_resolver() => _resolver.ShouldBeOfExactType<FixedResolver>();

    class FixedResolver : IGraphDatabaseNameResolver
    {
        public string Resolve() => "fixed";
    }
}
