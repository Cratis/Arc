// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_a_pending_policy_request_is_aborted : given.a_scenario_web_application
{
    PolicyGate _gate => Host!.Services.GetRequiredService<PolicyGate>();
    TenantFlowObservations _flows;
    bool _serverCancelled;
    int _performedBefore;

    void Establish()
    {
        _gate.Reset();
        _flows = Host!.Services.GetRequiredService<TenantFlowObservations>();
        _performedBefore = GatedReadModel.Performed;
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/gated-read-model");
        var flowId = Guid.NewGuid().ToString();
        request.Headers.Add("X-Default", "active");
        request.Headers.Add("X-Default-Tenant", "tenant-A");
        request.Headers.Add("X-PreResolve-Tenant", "true");
        request.Headers.Add("X-Controlled-Server-Abort", "true");
        request.Headers.Add("X-Flow-Id", flowId);
#pragma warning disable CA2025 // The pending request is awaited before the request and cancellation source are disposed.
        var pending = HttpClient!.SendAsync(request, cancellation.Token);
#pragma warning restore CA2025
        try
        {
            await _gate.WaitForEntry();
            _gate.HasExited.ShouldBeFalse();
            await _gate.AbortServerRequest();
            _serverCancelled = _gate.PolicyTokenCancelled;
            await _gate.WaitForExit();
            await _flows.Completed(flowId);
        }
        finally
        {
            _gate.Release();
            await cancellation.CancelAsync();
        }

        var transportFailure = await Catch.Exception(() => pending);
        if (transportFailure is null)
        {
            using var response = await pending;
        }
        else if (transportFailure is not OperationCanceledException and not HttpRequestException)
        {
            throw transportFailure;
        }
    }

    [Fact] void should_cancel_the_server_request() => _serverCancelled.ShouldBeTrue();
    [Fact] void should_not_invoke_the_query_after_cancellation() => GatedReadModel.Performed.ShouldEqual(_performedBefore);
}
