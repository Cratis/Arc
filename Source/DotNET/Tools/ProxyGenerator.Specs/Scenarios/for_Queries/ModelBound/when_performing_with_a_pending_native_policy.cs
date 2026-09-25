// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_with_a_pending_native_policy : given.a_scenario_web_application
{
    PolicyGate _gate => Host!.Services.GetRequiredService<PolicyGate>();
    QueryExecutionResult<GatedReadModel>? _result;
    bool _completedWhilePending;
    int _performedBefore;
    int _performedWhilePending;

    void Establish()
    {
        _gate.Reset();
        _performedBefore = GatedReadModel.Performed;
        LoadQueryProxy<GatedReadModel>("All");
        HttpClient!.DefaultRequestHeaders.Add("X-Default", "active");
    }

    async Task Because()
    {
        var pending = Bridge!.PerformQueryViaProxyAsync<GatedReadModel>("All");
        try
        {
            var entered = _gate.WaitForEntry();
            if (await Task.WhenAny(entered, pending) == pending)
            {
                var premature = await pending;
                throw new InvalidOperationException($"Query completed before policy entry: {premature.RawJson}");
            }

            await entered;
            _completedWhilePending = pending.IsCompleted;
            _performedWhilePending = GatedReadModel.Performed;
        }
        finally
        {
            _gate.Release();
        }

        _result = await pending.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_wait_for_the_policy() => _completedWhilePending.ShouldBeFalse();
    [Fact] void should_not_perform_before_authorization() => _performedWhilePending.ShouldEqual(_performedBefore);
    [Fact] void should_perform_after_authorization() => GatedReadModel.Performed.ShouldEqual(_performedBefore + 1);
    [Fact] void should_authorize_after_release() => _result!.Result!.IsAuthorized.ShouldBeTrue();
}
