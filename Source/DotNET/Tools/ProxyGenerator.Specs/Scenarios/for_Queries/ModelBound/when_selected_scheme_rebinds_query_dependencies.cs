// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_selected_scheme_rebinds_query_dependencies : given.a_scenario_web_application
{
    QueryExecutionResult<PolicyProtectedReadModel>? _result;
    TenantFlowObservations _observations;
    SelectedQueryFilterObservations _filterObservations;
    SelectedAuthorizationQueryFilterObservations _authorizationFilterObservations;
    string _flowId;
    (string Before, string After, Guid BeforeService, Guid AfterService) _flow;
    int _performedBefore;

    void Establish()
    {
        _performedBefore = PolicyProtectedReadModel.Performed;
        _observations = Host!.Services.GetRequiredService<TenantFlowObservations>();
        _filterObservations = Host.Services.GetRequiredService<SelectedQueryFilterObservations>();
        _filterObservations.Reset();
        _authorizationFilterObservations = Host.Services.GetRequiredService<SelectedAuthorizationQueryFilterObservations>();
        _authorizationFilterObservations.Reset();
        _flowId = Guid.NewGuid().ToString();
        LoadQueryProxy<PolicyProtectedReadModel>("All");
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Default-Tenant", "tenant-A");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special-Tenant", "tenant-B");
        HttpClient.DefaultRequestHeaders.Add("X-PreResolve-Tenant", "true");
        HttpClient.DefaultRequestHeaders.Add("X-Flow-Id", _flowId);
    }

    async Task Because()
    {
        _result = await Bridge!.PerformQueryViaProxyAsync<PolicyProtectedReadModel>("All");
        _flow = await _observations.Completed(_flowId);
    }

    [Fact] void should_authorize_the_query() => _result!.Result!.IsAuthorized.ShouldBeTrue();
    [Fact] void should_execute_once() => PolicyProtectedReadModel.Performed.ShouldEqual(_performedBefore + 1);
    [Fact] void should_resolve_a_fresh_selected_tenant_dependency() => PolicyProtectedReadModel.LastBoundTenant.ShouldEqual("tenant-B");
    [Fact] void should_rebind_http_request_services() => PolicyProtectedReadModel.LastHttpScopeTenant.ShouldEqual("tenant-B");
    [Fact] void should_construct_ordinary_filter_dependencies_after_authorization() => _filterObservations.Tenant.ShouldEqual("tenant-B");
    [Fact] void should_construct_authorization_filter_dependencies_under_selected_tenant() => _authorizationFilterObservations.ConstructedTenant.ShouldEqual("tenant-B");
    [Fact] void should_invoke_authorization_filter_under_selected_tenant() => _authorizationFilterObservations.ExecutedTenant.ShouldEqual("tenant-B");
    [Fact] void should_share_the_selected_scoped_dependency_with_the_query() =>
        _authorizationFilterObservations.ConstructedServiceId.ShouldEqual(PolicyProtectedReadModel.LastBoundServiceId);
    [Fact] void should_restore_the_original_tenant_cache() => _flow.After.ShouldEqual("tenant-A");
    [Fact] void should_restore_the_original_request_service() => _flow.AfterService.ShouldEqual(_flow.BeforeService);
}
