// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_an_explicit_provider_bound_before_scheme_selection : given.a_scenario_web_application
{
    TenantCommandObservations _observations;
    bool _authorized;
    string? _failureReason;
    int _handledBefore;

    void Establish()
    {
        _observations = Host!.Services.GetRequiredService<TenantCommandObservations>();
        _observations.Reset();
        _handledBefore = PolicyProtectedCommand.Handled;
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Default-Tenant", "tenant-A");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special-Tenant", "tenant-B");
        HttpClient.DefaultRequestHeaders.Add("X-PreResolve-Tenant", "true");
    }

    async Task Because()
    {
        using var response = await HttpClient!.PostAsync("/.cratis-test/explicit-scope", new StringContent("{}"));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _authorized = body.RootElement.GetProperty("isAuthorized").GetBoolean();
        _failureReason = body.RootElement.GetProperty("authorizationFailureReason").GetString();
    }

    [Fact] void should_reject_the_unsafe_explicit_provider() => _authorized.ShouldBeFalse();
    [Fact] void should_not_expose_scope_configuration_details() => _failureReason.ShouldBeEmpty();
    [Fact] void should_not_build_command_context_values() => _observations.ValuesTenant.ShouldBeNull();
    [Fact] void should_not_begin_any_execution_scope() => _observations.BeginTenant.ShouldBeNull();
    [Fact] void should_not_handle_the_command() => PolicyProtectedCommand.Handled.ShouldEqual(_handledBefore);
}
