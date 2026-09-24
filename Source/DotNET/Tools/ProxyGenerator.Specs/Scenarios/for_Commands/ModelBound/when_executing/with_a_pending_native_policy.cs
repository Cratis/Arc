// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_a_pending_native_policy : given.a_scenario_web_application
{
    PolicyGate _gate => Host!.Services.GetRequiredService<PolicyGate>();
    CommandResult<object>? _result;
    bool _completedWhilePending;
    int _handledBefore;
    int _handledWhilePending;

    void Establish()
    {
        _gate.Reset();
        _handledBefore = GatedCommand.Handled;
        LoadCommandProxy<GatedCommand>();
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
    }

    async Task Because()
    {
        var pending = Bridge!.ExecuteCommandViaProxyAsync<object>(new GatedCommand());
        try
        {
            await _gate.WaitForEntry();
            _completedWhilePending = pending.IsCompleted;
            _handledWhilePending = GatedCommand.Handled;
        }
        finally
        {
            _gate.Release();
        }

        _result = (await pending.WaitAsync(TimeSpan.FromSeconds(10))).Result;
    }

    [Fact] void should_wait_for_the_policy() => _completedWhilePending.ShouldBeFalse();
    [Fact] void should_not_handle_before_authorization() => _handledWhilePending.ShouldEqual(_handledBefore);
    [Fact] void should_handle_after_authorization() => GatedCommand.Handled.ShouldEqual(_handledBefore + 1);
    [Fact] void should_authorize_after_release() => _result!.IsAuthorized.ShouldBeTrue();
}
