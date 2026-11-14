// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Arc.Neo4j.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Neo4j.Chronicle.for_Neo4jGraphQueryExecutor.when_executing;

public class with_a_cancelled_query : Specification
{
    GraphSessionSubstitute _session;
    Exception _error;

    void Establish() => _session = new GraphSessionSubstitute();

    async Task Because() => _error = await Catch.Exception(() =>
        new Neo4jGraphQueryExecutor(_session, NullLogger<Neo4jGraphQueryExecutor>.Instance).Execute(OrganizationViews.Query("GroupSummary"), new CancellationToken(true)));

    [Fact] void should_be_cancelled() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_run_the_query() => _session.Executions.ShouldBeEmpty();
}
