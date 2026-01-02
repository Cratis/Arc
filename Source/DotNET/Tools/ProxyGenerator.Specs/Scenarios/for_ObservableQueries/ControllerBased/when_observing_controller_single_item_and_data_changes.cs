// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_observing_controller_single_item_and_data_changes : given.a_scenario_web_application
{
    ObservableQueryExecutionContext<ObservableControllerQueryItem>? _executionResult;
    ObservableControllerQueryItem? _updatedData;

    void Establish()
    {
        LoadControllerQueryProxy<ObservableControllerQueriesController>("ObserveSingle");
        _updatedData = new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66), Name = "Updated Single Controller Item", Value = 84 };
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<ObservableControllerQueryItem>(
            "ObserveSingle",
            updateReceiver: data => HttpClient.PostAsJsonAsync("/api/observable-controller-queries/update/single", data).Wait());

        // Trigger update and wait for notification
        await _executionResult.UpdateAndWaitAsync(_updatedData);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_received_update() => _executionResult.Updates.ShouldNotBeEmpty();
    [Fact] void should_have_updated_name() => _executionResult.LatestData.Name.ShouldEqual("Updated Single Controller Item");
    [Fact] void should_have_updated_value() => _executionResult.LatestData.Value.ShouldEqual(84);
}
