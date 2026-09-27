// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_changed_requirements_and_no_authorization_filter : given.a_query_pipeline
{
    ChangingRequirements _evaluator;
    QueryResult _result;

    void Establish()
    {
        var name = (FullyQualifiedQueryName)"CustomPolicyQuery.Load";
        _queryPerformer.Type.Returns(typeof(CustomPolicyQuery));
        _queryPerformer.Name.Returns((QueryName)"Load");
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });

        _evaluator = new ChangingRequirements();
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([_evaluator]));
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(declarations);

        // This is a replacement filter, not Arc's authorization filter. Its lookup sees no policy.
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_ =>
        {
            declarations.For(typeof(CustomPolicyQuery).GetMethod(nameof(CustomPolicyQuery.Load))).RequiresAsynchronousEvaluation.ShouldBeFalse();
            return Task.FromResult(QueryResult.Success(_correlationId));
        });
    }

    async Task Because() => _result = await _pipeline.Perform(
        "CustomPolicyQuery.Load", new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_read_the_changed_requirements_before_invocation() => _evaluator.Reads.ShouldEqual(2);
    [Fact] void should_deny_the_new_policy() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_performer() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());

    public class CustomPolicyQuery
    {
        public object Load() => new();
    }

    class ChangingRequirements : IAuthorizationAttributeEvaluator
    {
        public int Reads { get; private set; }

        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) =>
            method.DeclaringType == typeof(CustomPolicyQuery) && method.Name == nameof(CustomPolicyQuery.Load) && ++Reads > 1
                ? [AuthorizationRequirement.FromAttribute(null, "NewPolicy", null)] : [];
    }
}
