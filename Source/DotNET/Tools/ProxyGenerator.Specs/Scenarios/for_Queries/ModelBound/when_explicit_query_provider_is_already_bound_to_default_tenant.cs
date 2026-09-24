// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_explicit_query_provider_is_already_bound_to_default_tenant : given.a_scenario_web_application
{
    bool _authorized;
    string _failure = string.Empty;
    int _performedBefore;
    (string Before, string After, Guid BeforeService, Guid AfterService) _flow;
    string _flowId;
    TenantFlowObservations _observations;

    void Establish()
    {
        _performedBefore = PolicyProtectedReadModel.Performed;
        _observations = Host!.Services.GetRequiredService<TenantFlowObservations>();
        _flowId = Guid.NewGuid().ToString();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Default-Tenant", "tenant-A");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special-Tenant", "tenant-B");
        HttpClient.DefaultRequestHeaders.Add("X-PreResolve-Tenant", "true");
        HttpClient.DefaultRequestHeaders.Add("X-Flow-Id", _flowId);
    }

    async Task Because()
    {
        using var response = await HttpClient!.GetAsync("/.cratis-test/explicit-query-scope");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _authorized = body.RootElement.GetProperty("isAuthorized").GetBoolean();
        _failure = body.RootElement.GetProperty("exceptionMessages")[0].GetString() ?? string.Empty;
        _flow = await _observations.Completed(_flowId);
    }

    [Fact] void should_reject_the_explicit_provider() => _authorized.ShouldBeFalse();
    [Fact] void should_explain_that_a_fresh_scope_is_required() => _failure.ShouldContain("Arc-owned fresh service scope");
    [Fact] void should_not_perform_the_query() => PolicyProtectedReadModel.Performed.ShouldEqual(_performedBefore);
    [Fact] void should_restore_the_default_request_scope() => _flow.AfterService.ShouldEqual(_flow.BeforeService);
    [Fact] void should_restore_the_default_tenant() => _flow.After.ShouldEqual("tenant-A");
}
