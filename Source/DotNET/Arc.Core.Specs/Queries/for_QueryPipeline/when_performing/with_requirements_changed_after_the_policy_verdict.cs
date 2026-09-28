// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_requirements_changed_after_the_policy_verdict : given.a_query_pipeline
{
    ChangingRequirements _requirements;
    QueryResult _result;
    bool _performed;
    System.Diagnostics.ActivitySource _source;

    void Establish()
    {
        _requirements = new ChangingRequirements();
        var anonymous = new KnownInstancesOf<IAnonymousEvaluator>([]);
        var attributes = new KnownInstancesOf<IAuthorizationAttributeEvaluator>([_requirements]);
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test")));
        var evaluator = new AuthorizationEvaluator(principal, anonymous, attributes);
        var evaluation = new AuthorizationEvaluation(
            declarations,
            evaluator,
            principal,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Allow", typeof(AllowingPolicy))]));
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns(declarations);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principal);
        _serviceProvider.GetService(typeof(AllowingPolicy)).Returns(new AllowingPolicy());
        _serviceProvider.GetService(typeof(AuthorizationEvaluation)).Returns(evaluation);

        var method = typeof(ProtectedQuery).GetMethod(nameof(ProtectedQuery.Load))!;
        var performer = new ModelBoundQueryPerformer(
            typeof(ProtectedQuery),
            typeof(ProtectedQuery).FullName!,
            method,
            Substitute.For<IServiceProviderIsService>(),
            evaluator);
        _queryPerformerProviders.TryGetPerformersFor(performer.FullyQualifiedName, out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        var filters = Substitute.For<IStagedQueryFilters>();
        filters.Authorize(Arg.Any<QueryContext>()).Returns(async call =>
        {
            var context = call.Arg<QueryContext>();
            return await evaluation.IsAuthorized(method, context, _serviceProvider, CancellationToken.None)
                ? QueryResult.Success(context.CorrelationId) : QueryResult.Unauthorized(context.CorrelationId);
        });
        filters.AfterAuthorization(Arg.Any<QueryContext>()).Returns(call =>
        {
            _requirements.UseAdminRole = true;
            return Task.FromResult(QueryResult.Success(call.Arg<QueryContext>().CorrelationId));
        });
        var activity = Substitute.For<IActivitySource<QueryPipeline>>();
        _source = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activity.ActualSource.Returns(_source);
        _pipeline = new QueryPipeline(
            _correlationIdAccessor,
            _queryContextManager,
            filters,
            _queryPerformerProviders,
            _queryRenderers,
            _readModelInterceptors,
            _discoverableValidators,
            activity);
    }

    async Task Because()
    {
        ProtectedQuery.OnPerform = () => _performed = true;
        try
        {
            _result = await _pipeline.Perform(
                (FullyQualifiedQueryName)$"{typeof(ProtectedQuery).FullName}.Load",
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                _serviceProvider);
        }
        finally
        {
            ProtectedQuery.OnPerform = null;
        }
    }

    void Cleanup() => _source.Dispose();

    [Fact] void should_deny_a_verdict_for_the_old_policy() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_query() => _performed.ShouldBeFalse();

    public record ProtectedQuery
    {
        public static Action? OnPerform { get; set; }
        public static ProtectedQuery Load()
        {
            OnPerform?.Invoke();
            return new();
        }
    }

    class ChangingRequirements : IAuthorizationAttributeEvaluator
    {
        public bool UseAdminRole { get; set; }
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) => null;
        public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) => null;
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) => method.DeclaringType == typeof(ProtectedQuery)
            ? [AuthorizationRequirement.FromAttribute(UseAdminRole ? "Admin" : null, UseAdminRole ? null : "Allow", null)] : [];
    }

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
