// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_a_direct_stream_disconnects_during_an_emission : given.a_scenario_web_application
{
    [Fact]
    public async Task should_drain_a_sse_guard_before_releasing_the_request()
    {
        var observations = Prepare();
        var gate = Host!.Services.GetRequiredService<PolicyGate>();
        var flowId = Guid.NewGuid().ToString();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/policy-protected-stream");
        SetHeaders(request.Headers, flowId);
        request.Headers.Add("Accept", "text/event-stream");
        using var response = await HttpClient!.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        (await reader.ReadLineAsync(timeout.Token)).ShouldContain("data: ");
        observations.Reset();
        observations.PauseNext();

        PolicyProtectedStream.Emit();
        await observations.WaitForPause();
        try
        {
            await gate.AbortServerRequest();
            var completed = Host.Services.GetRequiredService<TenantFlowObservations>().Completed(flowId);
            completed.IsCompleted.ShouldBeFalse();
            observations.Release();
            var emission = await observations.Next();
            emission.NativePrincipal.ShouldEqual("Special");
            emission.NativeTenant.ShouldEqual("tenant-B");
            (await completed).After.ShouldEqual("tenant-A");
        }
        finally
        {
            observations.Release();
        }
    }

    [Fact]
    public async Task should_drain_a_websocket_guard_before_releasing_the_request()
    {
        var observations = Prepare();
        var gate = Host!.Services.GetRequiredService<PolicyGate>();
        var flowId = Guid.NewGuid().ToString();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("X-Controlled-Server-Abort", "yes");
        socket.Options.SetRequestHeader("X-PreResolve-Tenant", "yes");
        socket.Options.SetRequestHeader("X-Flow-Id", flowId);
        socket.Options.SetRequestHeader("X-Default", "active");
        socket.Options.SetRequestHeader("X-Default-Tenant", "tenant-A");
        socket.Options.SetRequestHeader("X-Special", "active");
        socket.Options.SetRequestHeader("X-Special-Tenant", "tenant-B");
        var uri = new Uri($"{ServerUrl!.Replace("http://", "ws://", StringComparison.Ordinal)}/api/policy-protected-stream");
        await socket.ConnectAsync(uri, timeout.Token);
        var buffer = new byte[8192];
        await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
        observations.Reset();
        observations.PauseNext();

        PolicyProtectedStream.Emit();
        await observations.WaitForPause();
        try
        {
            await gate.AbortServerRequest();
            var completed = Host.Services.GetRequiredService<TenantFlowObservations>().Completed(flowId);
            completed.IsCompleted.ShouldBeFalse();
            observations.Release();
            var emission = await observations.Next();
            emission.NativePrincipal.ShouldEqual("Special");
            emission.NativeTenant.ShouldEqual("tenant-B");
            (await completed).After.ShouldEqual("tenant-A");
        }
        finally
        {
            observations.Release();
        }
    }

    SelectedEmissionObservations Prepare()
    {
        PolicyProtectedStream.Reset();
        var gate = Host!.Services.GetRequiredService<PolicyGate>();
        gate.Reset();
        var observations = Host.Services.GetRequiredService<SelectedEmissionObservations>();
        observations.Reset();
        return observations;
    }

    static void SetHeaders(System.Net.Http.Headers.HttpRequestHeaders headers, string flowId)
    {
        headers.Add("X-Controlled-Server-Abort", "yes");
        headers.Add("X-PreResolve-Tenant", "yes");
        headers.Add("X-Flow-Id", flowId);
        headers.Add("X-Default", "active");
        headers.Add("X-Default-Tenant", "tenant-A");
        headers.Add("X-Special", "active");
        headers.Add("X-Special-Tenant", "tenant-B");
    }
}
