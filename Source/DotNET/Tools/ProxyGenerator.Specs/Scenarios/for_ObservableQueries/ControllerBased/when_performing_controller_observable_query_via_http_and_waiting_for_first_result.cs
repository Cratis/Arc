// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_controller_observable_query_via_http_and_waiting_for_first_result : given.a_scenario_web_application
{
    QueryExecutionResult<ObservableControllerQueryItem>? _executionResult;

    void Establish() => LoadControllerQueryProxy<ObservableControllerQueriesController>("ObserveDelayedSingle");

    async Task Because()
    {
        var performTask = Bridge.PerformQueryViaProxyAsync<ObservableControllerQueryItem>(
            "ObserveDelayedSingle",
            new Dictionary<string, object>
            {
                [ObservableQueryHttp.WaitForFirstResultQueryStringKey] = true,
                [ObservableQueryHttp.WaitForFirstResultTimeoutQueryStringKey] = 1
            });

        await Task.Delay(100);

        var state = Host.Services.GetRequiredService<ObservableControllerQueriesState>();
        state.DelayedSingleItemSubject.OnNext(new ObservableControllerQueryItem
        {
            Id = Guid.NewGuid(),
            Name = "Delayed Controller Item",
            Value = 321
        });

        _executionResult = await performTask;
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.RawJson.ShouldContain("\"data\"");
    [Fact] void should_have_waited_for_the_first_result() => GetDataName().ShouldEqual("Delayed Controller Item");

    string GetDataName() => JsonDocument.Parse(_executionResult.RawJson).RootElement.GetProperty("data").GetProperty("name").GetString()!;
}
