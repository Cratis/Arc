// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_observable_query_via_http_and_waiting_for_first_result : given.a_scenario_web_application
{
    QueryExecutionResult<ObservableReadModel>? _executionResult;

    void Establish() => LoadQueryProxy<ObservableReadModel>("ObserveDelayedSingle");

    async Task Because()
    {
        var performTask = Bridge.PerformQueryViaProxyAsync<ObservableReadModel>(
            "ObserveDelayedSingle",
            new Dictionary<string, object>
            {
                [ObservableQueryHttp.WaitForFirstResultQueryStringKey] = true,
                [ObservableQueryHttp.WaitForFirstResultTimeoutQueryStringKey] = 1
            });

        await Task.Delay(100);
        ObservableReadModel.UpdateDelayedSingleItem(new ObservableReadModel
        {
            Id = Guid.NewGuid(),
            Name = "Delayed Observable Item",
            Value = 123
        });

        _executionResult = await performTask;
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.RawJson.ShouldContain("\"data\"");
    [Fact] void should_have_waited_for_the_first_result() => GetDataName().ShouldEqual("Delayed Observable Item");

    string GetDataName() => JsonDocument.Parse(_executionResult.RawJson).RootElement.GetProperty("data").GetProperty("name").GetString()!;
}
