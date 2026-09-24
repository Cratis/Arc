// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_a_direct_sse_stream_emits_after_authorization : given.a_scenario_web_application
{
    SelectedEmissionObservations _observations;
    EmissionObservation _later;
    EmissionObservation _intercepted;

    void Establish()
    {
        PolicyProtectedStream.Reset();
        _observations = Host!.Services.GetRequiredService<SelectedEmissionObservations>();
        _observations.Reset();
        Host.Services.GetRequiredService<SelectedStreamInterceptorObservations>().Reset();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Default-Tenant", "tenant-A");
        HttpClient.DefaultRequestHeaders.Add("X-Special", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Special-Tenant", "tenant-B");
    }

    async Task Because()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/policy-protected-stream");
        request.Headers.Add("Accept", "text/event-stream");
        using var response = await HttpClient!.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.IsSuccessStatusCode.ShouldBeTrue();
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        (await ReadEmission(reader, timeout.Token)).ShouldContain("data: ");
        _observations.Reset();

        PolicyProtectedStream.Emit();
        (await ReadEmission(reader, timeout.Token)).ShouldContain("data: ");
        _later = await _observations.Next();
        _intercepted = await Host!.Services.GetRequiredService<SelectedStreamInterceptorObservations>().Next();
    }

    [Fact] void should_guard_with_the_selected_identity() => _later.Principal.ShouldEqual("Special");
    [Fact] void should_restore_the_ambient_principal() => _later.AmbientPrincipal.ShouldEqual("Special");
    [Fact] void should_restore_the_selected_tenant() => _later.Tenant.ShouldEqual("tenant-B");
    [Fact] void should_construct_guard_services_under_the_selected_tenant() => _later.ScopedTenant.ShouldEqual("tenant-B");
    [Fact] void should_expose_a_live_isolated_native_identity() => _later.NativePrincipal.ShouldEqual("Special");
    [Fact] void should_rebind_native_request_services() => _later.NativeTenant.ShouldEqual("tenant-B");
    [Fact] void should_construct_interceptors_under_the_selected_tenant() => _intercepted.ScopedTenant.ShouldEqual("tenant-B");
    [Fact] void should_expose_the_selected_native_principal_to_interceptors() => _intercepted.NativePrincipal.ShouldEqual("Special");

    static async Task<string> ReadEmission(StreamReader reader, CancellationToken token)
    {
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return line;
            }
        }

        throw new InvalidOperationException("Direct SSE stream closed before the expected emission.");
    }
}
