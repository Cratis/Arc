// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_ambiguous_hosted_authorization : given.a_query_pipeline
{
    QueryResult _result;

    void Establish()
    {
        _queryPerformer.Type.Returns(typeof(ConflictingQuery));
        _queryPerformer.Name.Returns((QueryName)nameof(ConflictingQuery.Load));
        _queryPerformerProviders.TryGetPerformersFor((FullyQualifiedQueryName)"ConflictingQuery.Load", out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        var anonymous = Substitute.For<IAnonymousEvaluator>();
        anonymous.IsAnonymousAllowed(typeof(ConflictingQuery)).Returns(true);
        var restricted = Substitute.For<IAnonymousEvaluator>();
        restricted.IsAnonymousAllowed(typeof(ConflictingQuery)).Returns(false);
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([anonymous, restricted]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])));
    }

    async Task Because() => _result = await _pipeline.PerformHosted(
        (FullyQualifiedQueryName)"ConflictingQuery.Load",
        QueryArguments.Empty,
        Paging.NotPaged,
        Sorting.None,
        _serviceProvider,
        CancellationToken.None);

    [Fact] void should_report_unauthorized_rather_than_an_error() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_run_filters() => query_filters.DidNotReceive().OnPerform(Arg.Any<QueryContext>());
    [Fact] void should_not_invoke_the_performer() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());

    public class ConflictingQuery
    {
        public object Load() => new();
    }
}
