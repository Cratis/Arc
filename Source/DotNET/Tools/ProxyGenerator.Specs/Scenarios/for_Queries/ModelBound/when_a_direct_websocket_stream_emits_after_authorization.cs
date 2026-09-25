// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_a_direct_websocket_stream_emits_after_authorization : given.a_scenario_web_application
{
    EmissionObservation _later;
    EmissionObservation _intercepted;

    void Establish()
    {
        PolicyProtectedStream.Reset();
        Host!.Services.GetRequiredService<SelectedEmissionObservations>().Reset();
        Host.Services.GetRequiredService<SelectedStreamInterceptorObservations>().Reset();
    }

    async Task Because()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("X-Default", "active");
        socket.Options.SetRequestHeader("X-Default-Tenant", "tenant-A");
        socket.Options.SetRequestHeader("X-Special", "active");
        socket.Options.SetRequestHeader("X-Special-Tenant", "tenant-B");
        var uri = new Uri($"{ServerUrl!.Replace("http://", "ws://", StringComparison.Ordinal)}/api/policy-protected-stream");
        await socket.ConnectAsync(uri, timeout.Token);
        await Read(socket, timeout.Token);
        var observations = Host.Services.GetRequiredService<SelectedEmissionObservations>();
        observations.Reset();
        PolicyProtectedStream.Emit();
        await Read(socket, timeout.Token);
        _later = await observations.Next();
        _intercepted = await Host.Services.GetRequiredService<SelectedStreamInterceptorObservations>().Next();
    }

    [Fact] void should_guard_as_b() => _later.Principal.ShouldEqual("Special");
    [Fact] void should_preserve_the_ambient_b_principal() => _later.AmbientPrincipal.ShouldEqual("Special");
    [Fact] void should_preserve_the_b_tenant() => _later.Tenant.ShouldEqual("tenant-B");
    [Fact] void should_resolve_guard_dependencies_in_b() => _later.ScopedTenant.ShouldEqual("tenant-B");
    [Fact] void should_expose_a_live_native_b_principal() => _later.NativePrincipal.ShouldEqual("Special");
    [Fact] void should_rebind_the_live_native_provider_to_b() => _later.NativeTenant.ShouldEqual("tenant-B");
    [Fact] void should_construct_interceptors_under_b() => _intercepted.ScopedTenant.ShouldEqual("tenant-B");
    [Fact] void should_expose_b_natively_to_interceptors() => _intercepted.NativePrincipal.ShouldEqual("Special");

    static async Task<QueryResult> Read(ClientWebSocket socket, CancellationToken token)
    {
        var buffer = new byte[8192];
        var frame = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
        return JsonSerializer.Deserialize<QueryResult>(new ReadOnlySpan<byte>(buffer, 0, frame.Count), Json.Globals.JsonSerializerOptions)!;
    }
}
