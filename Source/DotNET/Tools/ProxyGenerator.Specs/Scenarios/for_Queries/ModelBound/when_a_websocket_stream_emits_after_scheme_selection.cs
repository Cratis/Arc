// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_a_websocket_stream_emits_after_scheme_selection : given.a_scenario_web_application
{
    SelectedEmissionObservations _observations;
    EmissionObservation _later;
    bool _guardRegistered;
    string _outerTenant;

    void Establish()
    {
        PolicyProtectedStream.Reset();
        _observations = Host!.Services.GetRequiredService<SelectedEmissionObservations>();
        _observations.Reset();
        _guardRegistered = Host.Services.GetRequiredService<IObservableQueryEmissionGuards>().HasGuards;
    }

    async Task Because()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("X-Default", "active");
        socket.Options.SetRequestHeader("X-Default-Tenant", "tenant-A");
        socket.Options.SetRequestHeader("X-Special", "active");
        socket.Options.SetRequestHeader("X-Special-Tenant", "tenant-B");
        socket.Options.SetRequestHeader("X-PreResolve-Tenant", "true");
        var uri = new Uri($"{ServerUrl!.Replace("http://", "ws://", StringComparison.Ordinal)}/.cratis/queries/ws");
        await socket.ConnectAsync(uri, timeout.Token);
        await Read(socket, timeout.Token); // Connected frame.

        var subscribe = new ObservableQueryHubMessage
        {
            Type = ObservableQueryHubMessageType.Subscribe,
            QueryId = "stream",
            Payload = new ObservableQuerySubscriptionRequest($"{typeof(PolicyProtectedStream).FullName}.Watch")
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(subscribe, Json.Globals.JsonSerializerOptions);
        await socket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, timeout.Token);
        var initial = await Read(socket, timeout.Token);
        initial.Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
        _observations.Reset();

        var accessor = Host.Services.GetRequiredService<IHttpRequestContextAccessor>();
        var previous = accessor.Current;
        var simulatedProducer = Substitute.For<IHttpRequestContext>();
        simulatedProducer.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "tenant-A")], "Default")));
        accessor.Current = simulatedProducer;
        try
        {
            _outerTenant = Host.Services.GetRequiredService<ITenantIdAccessor>().Current.Value;
            PolicyProtectedStream.Emit();
            var laterFrame = await Read(socket, timeout.Token);
            laterFrame.Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
            _later = await _observations.Next();
        }
        finally
        {
            accessor.Current = previous;
        }
    }

    [Fact] void should_discover_the_real_guard() => _guardRegistered.ShouldBeTrue();
    [Fact] void should_start_from_the_other_cached_tenant() => _outerTenant.ShouldEqual("tenant-A");
    [Fact] void should_guard_as_the_selected_scheme_principal() => _later.Principal.ShouldEqual("Special");
    [Fact] void should_guard_under_the_selected_tenant_cache() => _later.Tenant.ShouldEqual("tenant-B");
    [Fact] void should_construct_emission_dependencies_under_the_selected_tenant() => _later.ScopedTenant.ShouldEqual("tenant-B");
    [Fact] void should_restore_the_ambient_subscriber_principal() => _later.AmbientPrincipal.ShouldEqual("Special");
    [Fact] void should_not_expose_a_native_connection_context_to_later_emissions() => _later.NativePrincipal.ShouldBeNull();

    static async Task<ObservableQueryHubMessage> Read(ClientWebSocket socket, CancellationToken token)
    {
        var buffer = new byte[8192];
        var frame = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
        return JsonSerializer.Deserialize<ObservableQueryHubMessage>(new ReadOnlySpan<byte>(buffer, 0, frame.Count), Json.Globals.JsonSerializerOptions)!;
    }
}
