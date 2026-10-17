// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_a_snapshot_guard_observes_the_selected_identity : given.a_guarded_websocket_connection
{
    CurrentPrincipalAccessor _principals;
    TenantIdAccessor _tenants;
    ClaimsPrincipal _selectedPrincipal;
    ClaimsPrincipal? _guardPrincipal;
    ClaimsPrincipal? _restoredPrincipal;
    TenantId? _guardTenant;
    TenantId? _restoredTenant;

    void Establish()
    {
        _selectedPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "selected-caller")], "selected-scheme"));
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var result = QueryResult.Success(CorrelationId.New());
                result.Data = new[] { "snapshot" };
                result.AuthorizedPrincipal = _selectedPrincipal;
                result.AuthorizedTenant = new TenantId("selected-tenant");
                return Task.FromResult(result);
            });
        _verdict = context =>
        {
            _guardPrincipal = context.ServiceProvider.GetRequiredService<CurrentPrincipalAccessor>().Current;
            _guardTenant = context.ServiceProvider.GetRequiredService<TenantIdAccessor>().Current;
            return ObservableQueryEmissionVerdict.Allow;
        };
    }

    async Task Because()
    {
        using var tenant = _tenants.Begin(new TenantId("outer-tenant"));
        using var principal = _principals.UseAuthorizationPrincipal(_principal);
        await RunConnection(() => Task.CompletedTask);
        _restoredPrincipal = _principals.Current;
        _restoredTenant = _tenants.Current;
    }

    [Fact] void should_evaluate_the_guard_as_the_selected_caller() => _guardPrincipal!.Identity!.Name.ShouldEqual("selected-caller");
    [Fact] void should_evaluate_the_guard_in_the_selected_tenant() => _guardTenant.ShouldEqual(new TenantId("selected-tenant"));
    [Fact] void should_restore_the_outer_caller() => _restoredPrincipal.ShouldBeSame(_principal);
    [Fact] void should_restore_the_outer_tenant() => _restoredTenant.ShouldEqual(new TenantId("outer-tenant"));
    [Fact] void should_deliver_the_snapshot() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_identify_the_snapshot() => _guardCalls.Single().IsSnapshot.ShouldBeTrue();

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        _principals = new CurrentPrincipalAccessor(_httpRequestContextAccessor);
        _tenants = new TenantIdAccessor(Substitute.For<ITenantIdResolver>());
        services.AddSingleton(_principals);
        services.AddSingleton(_tenants);
    }
}
