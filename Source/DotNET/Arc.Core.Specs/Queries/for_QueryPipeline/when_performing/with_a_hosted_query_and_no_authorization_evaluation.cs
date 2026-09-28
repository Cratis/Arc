// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_hosted_query_and_no_authorization_evaluation : given.a_query_pipeline
{
    QueryResult _result;

    void Establish()
    {
        _queryPerformer.Type.Returns(typeof(ProtectedQuery));
        _queryPerformer.Name.Returns((QueryName)"Load");
        _queryPerformerProviders.TryGetPerformersFor((FullyQualifiedQueryName)"ProtectedQuery.Load", out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new PolicyDeclaration()])));
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns((object?)null);
    }

    async Task Because() => _result = await _pipeline.PerformHosted(
        (FullyQualifiedQueryName)"ProtectedQuery.Load",
        QueryArguments.Empty,
        Paging.NotPaged,
        Sorting.None,
        _serviceProvider,
        CancellationToken.None);

    [Fact] void should_report_unauthorized_rather_than_an_error() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_run_filters() => query_filters.DidNotReceive().OnPerform(Arg.Any<QueryContext>());
    [Fact] void should_not_invoke_the_performer() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());

    public class ProtectedQuery
    {
        public object Load() => new();
    }

    class PolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) =>
            method.DeclaringType == typeof(ProtectedQuery) && method.Name == nameof(ProtectedQuery.Load)
                ? [AuthorizationRequirement.FromAttribute(null, "Protected", null)] : [];
    }
}
