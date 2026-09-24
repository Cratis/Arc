// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_the_request_is_cancelled_during_a_command_policy : given.a_scenario_web_application
{
    PolicyGate _gate => Host!.Services.GetRequiredService<PolicyGate>();
    TenantFlowObservations _flows;
    bool _cooperativeServerCancelled;
    bool _nonCooperativeServerCancelled;
    int _cooperativeBefore;
    int _cooperativeAfter;
    int _nonCooperativeBefore;
    int _nonCooperativeAfter;

    void Establish()
    {
        _flows = Host.Services.GetRequiredService<TenantFlowObservations>();
    }

    async Task Because()
    {
        _cooperativeBefore = GatedCommand.Handled;
        _cooperativeServerCancelled = await Cancel(typeof(GatedCommand), cooperates: true);
        _cooperativeAfter = GatedCommand.Handled;
        _nonCooperativeBefore = NonCooperativeCommand.Handled;
        _nonCooperativeServerCancelled = await Cancel(typeof(NonCooperativeCommand), cooperates: false);
        _nonCooperativeAfter = NonCooperativeCommand.Handled;
    }

    [Fact] void should_cancel_the_cooperative_request_on_the_server() => _cooperativeServerCancelled.ShouldBeTrue();
    [Fact] void should_not_handle_after_a_cooperative_policy_is_cancelled() => _cooperativeAfter.ShouldEqual(_cooperativeBefore);
    [Fact] void should_cancel_the_noncooperative_request_on_the_server() => _nonCooperativeServerCancelled.ShouldBeTrue();
    [Fact] void should_not_handle_after_a_noncooperative_policy_returns_true() => _nonCooperativeAfter.ShouldEqual(_nonCooperativeBefore);

    async Task<bool> Cancel(Type commandType, bool cooperates)
    {
        _gate.Reset();
        using var cancellation = new CancellationTokenSource();
        var flowId = Guid.NewGuid().ToString();
        var pending = Send(commandType, flowId, cancellation.Token);
        try
        {
            await _gate.WaitForEntry();
            if (!_gate.PolicyUsesServerAbort)
            {
                throw new InvalidOperationException("The policy did not receive the controlled server request token.");
            }

            if (_gate.HasExited)
            {
                throw new InvalidOperationException("The policy completed before the cancellation test entered its gate.");
            }

            await _gate.AbortServerRequest();
            if (!_gate.PolicyTokenCancelled)
            {
                throw new InvalidOperationException("The installed policy token was not canceled by the server abort.");
            }
            if (!cooperates)
            {
                _gate.Release();
            }

            await _gate.WaitForExit();
            await _flows.Completed(flowId);
            await cancellation.CancelAsync();
            var transportFailure = await Catch.Exception(() => pending);
            if (transportFailure is null)
            {
                using var response = await pending;
            }
            else if (transportFailure is not OperationCanceledException and not HttpRequestException)
            {
                throw transportFailure;
            }

            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task<HttpResponseMessage> Send(Type commandType, string flowId, CancellationToken token)
    {
        var providers = Host!.Services.GetRequiredService<ICommandHandlerProviders>();
        var handlers = providers.Handlers.ToArray();
        var handler = handlers.Single(candidate => candidate.CommandType == commandType);
        var options = Host.Services.GetRequiredService<IOptions<ArcOptions>>().Value.GeneratedApis;
        var grouped = EndpointRouteHelper.GroupByNamespace(handlers, candidate => candidate.Location, options.SegmentsToSkipForRoute);
        var location = handler.Location.Skip(options.SegmentsToSkipForRoute);
        var includeName = EndpointRouteHelper.ShouldIncludeNameInRoute(options.IncludeCommandNameInRoute, location, grouped);
        var route = EndpointRouteHelper.BuildRouteUrl(options, handler.Location, options.SegmentsToSkipForRoute, commandType.Name, includeName);
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Default", "active");
        request.Headers.Add("X-Default-Tenant", "tenant-A");
        request.Headers.Add("X-PreResolve-Tenant", "true");
        request.Headers.Add("X-Controlled-Server-Abort", "true");
        request.Headers.Add("X-Flow-Id", flowId);
        return await HttpClient!.SendAsync(request, token);
    }
}
