// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryPipeline;

public class when_composing_without_the_arc_host : Specification
{
    readonly FullyQualifiedQueryName _name = "CompositionQuery";
    ServiceProvider _provider;
    ActivitySource _activitySource;
    AuthorizationPrincipalScope _principalScope;
    IQueryPerformer _performer;
    QueryResult _result;

    async Task Because()
    {
        var correlationId = CorrelationId.New();
        var correlationIds = Substitute.For<ICorrelationIdAccessor>();
        correlationIds.Current.Returns(correlationId);

        _performer = Substitute.For<IQueryPerformer>();
        _performer.Dependencies.Returns([]);
        _performer.Parameters.Returns(QueryParameters.Empty);
        _performer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(null));

        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.TryGetPerformersFor(_name, out Arg.Any<IQueryPerformer>()).Returns(call =>
        {
            call[1] = _performer;
            return true;
        });

        var filters = Substitute.For<IQueryFilters>();
        filters.OnPerform(Arg.Any<QueryContext>()).Returns(_ => QueryResult.Success(correlationId));
        _activitySource = new ActivitySource("Arc.Composition.Spec");
        var activitySource = Substitute.For<IActivitySource<QueryPipeline>>();
        activitySource.ActualSource.Returns(_activitySource);

        var services = new ServiceCollection();
        services.AddSingleton(correlationIds);
        services.AddSingleton<IQueryContextManager, QueryContextManager>();
        services.AddSingleton(filters);
        services.AddSingleton(performers);
        services.AddSingleton(Substitute.For<IQueryRenderers>());
        services.AddSingleton(Substitute.For<IReadModelInterceptors>());
        services.AddSingleton(Substitute.For<IDiscoverableValidators>());
        services.AddSingleton(activitySource);
        services.AddSingleton(Substitute.For<IHttpRequestContextAccessor>());
        services.AddSingleton(Substitute.For<IAuthorizationPolicyRuntime>());
        services.AddSingleton(Substitute.For<ITenantIdResolver>());
        services.AddSingleton<CurrentPrincipalAccessor>();
        services.AddSingleton<TenantIdAccessor>();
        services.AddTransient<AuthorizationPrincipalScope>();
        services.AddSingleton<IQueryPipeline, QueryPipeline>();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _principalScope = _provider.GetRequiredService<AuthorizationPrincipalScope>();
        await using var scope = _provider.CreateAsyncScope();
        _result = await _provider.GetRequiredService<IQueryPipeline>().Perform(
            _name, new QueryArguments(), Paging.NotPaged, Sorting.None, scope.ServiceProvider);
    }

    void Cleanup()
    {
        _provider?.Dispose();
        _activitySource?.Dispose();
    }

    [Fact] void should_resolve_the_public_principal_scope() => _principalScope.ShouldNotBeNull();
    [Fact] void should_execute_the_query() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_invoke_the_performer() => _performer.Received(1).Perform(Arg.Any<QueryContext>());
}
