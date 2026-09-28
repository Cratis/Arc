// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_ControllerQueryPerformer.when_performing;

public class with_a_guest_verdict_and_a_dependency_that_authenticates : Specification
{
    QueryResult _result;
    bool _invoked;
    bool _authenticatedAfterResolution;
    bool _verdictIssued;
    bool _dependencyResolved;
    IDisposable? _authenticatedScope;

    async Task Because()
    {
        var current = new ClaimsPrincipal(new ClaimsIdentity());
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(_ => current);
        var principalOverride = Substitute.For<ICurrentPrincipalOverride>();
        principalOverride.BeginScope(Arg.Any<ClaimsPrincipal>()).Returns(call =>
        {
            current = call.Arg<ClaimsPrincipal>();
            return Substitute.For<IDisposable>();
        });
        var anonymous = new KnownInstancesOf<IAnonymousEvaluator>([new AnonymousEvaluator()]);
        var attributes = new KnownInstancesOf<IAuthorizationAttributeEvaluator>([new AuthorizationAttributeEvaluator()]);
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var evaluator = new AuthorizationEvaluator(accessor, anonymous, attributes);
        var evaluation = new AuthorizationEvaluation(
            declarations,
            evaluator,
            accessor,
            new ArcAuthorizationPolicyRuntime([new AuthorizationPolicyRegistration("Guest", typeof(AllowingPolicy)) { EvaluatesAnonymous = true }]));
        await using var services = new ServiceCollection()
            .AddSingleton(declarations)
            .AddSingleton<ICurrentPrincipalAccessor>(accessor)
            .AddSingleton<ICurrentPrincipalOverride>(principalOverride)
            .AddSingleton<AllowingPolicy>()
            .AddTransient(_ =>
            {
                _dependencyResolved = true;
                _authenticatedScope = principalOverride.BeginScope(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "member")], "test")));
                _authenticatedAfterResolution = accessor.Current?.Identity?.IsAuthenticated == true;
                return new ChangingDependency();
            })
            .BuildServiceProvider();
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(ProtectedController.Load),
            ControllerName = nameof(ProtectedController),
            ControllerTypeInfo = typeof(ProtectedController).GetTypeInfo(),
            MethodInfo = typeof(ProtectedController).GetMethod(nameof(ProtectedController.Load))!
        };
        var performer = new ControllerQueryPerformer(descriptor, services.GetRequiredService<IServiceProviderIsService>(), evaluator);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.TryGetPerformersFor(performer.FullyQualifiedName, out var _).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        var filters = Substitute.For<IStagedQueryFilters>();
        filters.Authorize(Arg.Any<QueryContext>()).Returns(async call =>
        {
            var context = call.Arg<QueryContext>();
            var allowed = await evaluation.IsAuthorized(performer.AuthorizationMethod, context, services, CancellationToken.None, () => true);
            _verdictIssued = allowed && context.AuthorizedExecution is not null;
            return allowed ? QueryResult.Success(context.CorrelationId) : QueryResult.Unauthorized(context.CorrelationId);
        });
        filters.AfterAuthorization(Arg.Any<QueryContext>()).Returns(call => Task.FromResult(QueryResult.Success(call.Arg<QueryContext>().CorrelationId)));
        var source = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        var activity = Substitute.For<IActivitySource<QueryPipeline>>();
        activity.ActualSource.Returns(source);
        try
        {
            ProtectedController.OnInvoke = () => _invoked = true;
            var pipeline = new QueryPipeline(
                Substitute.For<ICorrelationIdAccessor>(),
                Substitute.For<IQueryContextManager>(),
                filters,
                performers,
                Substitute.For<IQueryRenderers>(),
                Substitute.For<IReadModelInterceptors>(),
                Substitute.For<Cratis.Arc.Validation.IDiscoverableValidators>(),
                activity);
            _result = await pipeline.Perform(performer.FullyQualifiedName, QueryArguments.Empty, Paging.NotPaged, Sorting.None, services);
        }
        finally
        {
            ProtectedController.OnInvoke = null;
            _authenticatedScope?.Dispose();
            source.Dispose();
        }
    }

    [Fact] void should_have_issued_the_guest_verdict() => _verdictIssued.ShouldBeTrue();
    [Fact] void should_resolve_the_dependency() => _dependencyResolved.ShouldBeTrue();
    [Fact] void should_authenticate_during_dependency_resolution() => _authenticatedAfterResolution.ShouldBeTrue();
    [Fact] void should_deny_the_authenticated_principal_after_the_guest_verdict() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_invoke_the_action() => _invoked.ShouldBeFalse();

    public class ChangingDependency;

    public class ProtectedController
    {
        public static Action? OnInvoke { get; set; }

        [Cratis.Arc.Authorization.Authorize(Policy = "Guest")]
        public string Load([FromServices] ChangingDependency dependency)
        {
            OnInvoke?.Invoke();
            return "unexpected";
        }
    }

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
