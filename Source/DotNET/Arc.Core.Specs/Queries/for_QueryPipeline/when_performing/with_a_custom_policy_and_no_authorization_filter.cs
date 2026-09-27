// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_custom_policy_and_no_authorization_filter : given.a_query_pipeline
{
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
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));

        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => Array.Empty<IAnonymousEvaluator>().AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new CustomPolicyDeclaration() }.AsEnumerable().GetEnumerator());
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(new AuthorizationDeclarations(anonymous, attributes));
    }

    async Task Because() => _result = await _pipeline.Perform(
        "CustomPolicyQuery.Load", new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_deny_the_query() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_performer() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());

    public class CustomPolicyQuery
    {
        public object Load() => new();
    }

    class CustomPolicyDeclaration : IAuthorizationAttributeEvaluator
    {
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) =>
            method.DeclaringType == typeof(CustomPolicyQuery) && method.Name == nameof(CustomPolicyQuery.Load)
                ? [AuthorizationRequirement.FromAttribute(null, "Custom", null)] : [];
    }
}
