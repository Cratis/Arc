// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Arc.Neo4j.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Neo4j.Chronicle.for_Neo4jGraphQueryExecutor.when_executing;

public class with_a_query_on_a_single_database : Specification
{
    GraphSessionSubstitute _session;

    void Establish() => _session = new GraphSessionSubstitute { Namespace = "acme", DatabaseMode = GraphDatabaseMode.Single };

    Task Because() => new Neo4jGraphQueryExecutor(_session, NullLogger<Neo4jGraphQueryExecutor>.Instance).Execute(OrganizationViews.Query("GroupSummary"));

    [Fact] void should_constrain_the_roots_to_the_namespace_of_the_session() => _session.Executions[0].Cypher.ShouldContain("WHERE n0.`_namespace` = $namespace\n");
    [Fact] void should_give_the_namespace_through_the_session() => _session.Executions[0].Parameters[GraphParameters.Namespace].ShouldEqual("acme");
}
