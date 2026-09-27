// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_hosted_query_and_no_authorization_declarations : given.a_query_pipeline
{
    QueryResult _result;

    void Establish()
    {
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);
        _queryPerformerProviders.TryGetPerformersFor((FullyQualifiedQueryName)"ProtectedQuery.Load", out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
    }

    async Task Because() => _result = await _pipeline.PerformHosted(
        (FullyQualifiedQueryName)"ProtectedQuery.Load",
        QueryArguments.Empty,
        Paging.NotPaged,
        Sorting.None,
        _serviceProvider,
        CancellationToken.None);

    [Fact] void should_report_unauthorized_rather_than_an_error() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_performer() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());
}
