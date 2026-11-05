// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Neo4j.for_ServiceCollectionExtensions;

public class when_adding_cratis_neo4j_with_a_resolver_that_is_not_one : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => new ServiceCollection().AddCratisNeo4j(configureNeo4j: neo4j => neo4j.DatabaseNameResolverType = typeof(string)));

    [Fact] void should_refuse() => _result.ShouldBeOfExactType<ArgumentException>();
}
