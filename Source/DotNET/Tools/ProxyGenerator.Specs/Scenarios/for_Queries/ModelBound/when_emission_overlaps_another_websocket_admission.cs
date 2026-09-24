// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_emission_overlaps_another_websocket_admission : given.a_scenario_web_application
{
    SelectedEmissionObservations _observations;
    PolicyGate _gate => Host!.Services.GetRequiredService<PolicyGate>();
    EmissionObservation _duringAdmission;

    void Establish()
    {
        PolicyProtectedStream.Reset();
        _observations = Host!.Services.GetRequiredService<SelectedEmissionObservations>();
        _observations.Reset();
        _gate.Reset();
    }

    async Task Because()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("X-Default", "active");
        socket.Options.SetRequestHeader("X-Default-Tenant", "tenant-A");
        socket.Options.SetRequestHeader("X-Special", "active");
        socket.Options.SetRequestHeader("X-Special-Tenant", "tenant-B");
        socket.Options.SetRequestHeader("X-Other", "active");
        socket.Options.SetRequestHeader("X-Other-Tenant", "tenant-C");
        socket.Options.SetRequestHeader("X-PreResolve-Tenant", "true");
        var uri = new Uri($"{ServerUrl!.Replace("http://", "ws://", StringComparison.Ordinal)}/.cratis/queries/ws");
        await socket.ConnectAsync(uri, timeout.Token);
        await Read(socket, timeout.Token); // Connected.

        await Subscribe(socket, "b", $"{typeof(PolicyProtectedStream).FullName}.Watch", timeout.Token);
        (await Read(socket, timeout.Token)).Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
        _observations.Reset();
        _observations.PauseNext();

        await Subscribe(socket, "c", $"{typeof(GatedSelectedReadModel).FullName}.All", timeout.Token);
        try
        {
            await _gate.WaitForEntry();
            PolicyProtectedStream.Emit();
            await _observations.WaitForPause();
        }
        finally
        {
            _observations.Release();
            _gate.Release();
        }

        _duringAdmission = await _observations.Next();
        await Read(socket, timeout.Token);
        await Read(socket, timeout.Token);
    }

    [Fact] void should_hold_an_other_identity_admission() => _gate.Visits.Single().User.ShouldEqual("Other");
    [Fact] void should_expose_c_in_the_isolated_native_admission() => _gate.SelectedNativePrincipal.ShouldEqual("Other");
    [Fact] void should_keep_the_underlying_connection_principal_a() => _gate.UnderlyingNativePrincipal.ShouldEqual("Default");
    [Fact] void should_expose_c_scoped_services_in_the_native_admission() => _gate.SelectedNativeTenant.ShouldEqual("tenant-C");
    [Fact] void should_keep_the_underlying_connection_provider_a() => _gate.UnderlyingNativeTenant.ShouldEqual("tenant-A");
    [Fact] void should_keep_the_b_ambient_principal_across_guard_await() => _duringAdmission.AmbientPrincipal.ShouldEqual("Special");
    [Fact] void should_keep_the_b_tenant_across_guard_await() => _duringAdmission.Tenant.ShouldEqual("tenant-B");
    [Fact] void should_not_expose_c_native_before_the_guard_await() => _duringAdmission.NativePrincipalBeforeAwait.ShouldBeNull();
    [Fact] void should_not_expose_c_native_after_the_guard_await() => _duringAdmission.NativePrincipal.ShouldBeNull();
    [Fact] void should_not_expose_c_provider_before_the_guard_await() => _duringAdmission.NativeTenantBeforeAwait.ShouldBeNull();
    [Fact] void should_not_expose_c_provider_after_the_guard_await() => _duringAdmission.NativeTenant.ShouldBeNull();

    static async Task Subscribe(ClientWebSocket socket, string id, string name, CancellationToken token)
    {
        var message = new ObservableQueryHubMessage
        {
            Type = ObservableQueryHubMessageType.Subscribe,
            QueryId = id,
            Payload = new ObservableQuerySubscriptionRequest(name)
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, Json.Globals.JsonSerializerOptions);
        await socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, token);
    }

    static async Task<ObservableQueryHubMessage> Read(ClientWebSocket socket, CancellationToken token)
    {
        var buffer = new byte[8192];
        var frame = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
        return JsonSerializer.Deserialize<ObservableQueryHubMessage>(
            new ReadOnlySpan<byte>(buffer, 0, frame.Count), Json.Globals.JsonSerializerOptions)!;
    }
}
