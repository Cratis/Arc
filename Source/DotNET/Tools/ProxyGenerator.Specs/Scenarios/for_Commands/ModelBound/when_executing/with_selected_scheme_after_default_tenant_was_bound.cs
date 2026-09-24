// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_selected_scheme_after_default_tenant_was_bound : given.a_scenario_web_application
{
    CommandResult<object>? _result;
    TenantFlowObservations _observations;
    TenantCommandObservations _commandObservations;
    string _flowId;
    (string Before, string After, Guid BeforeService, Guid AfterService) _flow;
    int _handledBefore;

    void Establish()
    {
        _handledBefore = PolicyProtectedCommand.Handled;
        _observations = Host!.Services.GetRequiredService<TenantFlowObservations>();
        _commandObservations = Host.Services.GetRequiredService<TenantCommandObservations>();
        _commandObservations.Reset();
        _flowId = Guid.NewGuid().ToString();
        LoadCommandProxy<PolicyProtectedCommand>();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Default-Tenant", "tenant-A");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special-Tenant", "tenant-B");
        HttpClient.DefaultRequestHeaders.Add("X-PreResolve-Tenant", "true");
        HttpClient.DefaultRequestHeaders.Add("X-Flow-Id", _flowId);
    }

    async Task Because()
    {
        _result = (await Bridge!.ExecuteCommandViaProxyAsync<object>(new PolicyProtectedCommand())).Result;
        _flow = await _observations.Completed(_flowId);
    }

    [Fact] void should_authorize_the_selected_caller() => _result!.IsAuthorized.ShouldBeTrue();
    [Fact] void should_execute_once() => PolicyProtectedCommand.Handled.ShouldEqual(_handledBefore + 1);
    [Fact] void should_bind_the_handler_dependency_to_the_selected_tenant() => PolicyProtectedCommand.LastBoundTenant.ShouldEqual("tenant-B");
    [Fact] void should_bind_http_request_services_to_the_selected_tenant() => PolicyProtectedCommand.LastRequestServicesTenant.ShouldEqual("tenant-B");
    [Fact] void should_not_reuse_the_service_bound_to_the_default_tenant() => PolicyProtectedCommand.ReusedOldTenantService.ShouldBeFalse();
    [Fact] void should_restore_the_original_tenant_cache() => _flow.After.ShouldEqual("tenant-A");
    [Fact] void should_restore_the_original_request_service() => _flow.AfterService.ShouldEqual(_flow.BeforeService);
    [Fact] void should_build_context_values_under_the_selected_tenant() => _commandObservations.ValuesTenant.ShouldEqual("tenant-B");
    [Fact] void should_bind_execution_scope_begin_to_the_selected_tenant() => _commandObservations.BeginTenant.ShouldEqual("tenant-B");
    [Fact] void should_complete_the_scope_under_the_selected_tenant() => _commandObservations.CompleteTenant.ShouldEqual("tenant-B");
}
