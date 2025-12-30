// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

public class when_observing_controller_all_items_and_data_changes : given.a_scenario_web_application
{
    ObservableQueryExecutionResult<IEnumerable<ObservableControllerQueryItem>>? _executionResult;
    ObservableControllerQueryItem[]? _updatedData;

    void Establish()
    {
        LoadControllerQueryProxy<ObservableControllerQueriesController>("ObserveAll");
        _updatedData =
        [
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), Name = "Updated Controller Item 1", Value = 10 },
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), Name = "Updated Controller Item 2", Value = 20 },
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4), Name = "New Controller Item 4", Value = 40 }
        ];
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ObservableControllerQueryItem>>("ObserveAll");

        // Update the data on the backend via HTTP POST
        await HttpClient.PostAsJsonAsync("/api/observable-controller-queries/update/items", _updatedData);

        // Sync observable updates to get fresh HTTP snapshot
        await Bridge.SyncObservableUpdates(_executionResult);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_received_update() => _executionResult.Updates.ShouldNotBeEmpty();
    [Fact] void should_have_three_items_in_latest_update() => _executionResult.LatestData.Count().ShouldEqual(3);
    [Fact] void should_have_new_item() => _executionResult.LatestData.Any(_ => _.Name == "New Controller Item 4").ShouldBeTrue();
    [Fact] void should_have_updated_values() => _executionResult.LatestData.First(_ => _.Name == "Updated Controller Item 1").Value.ShouldEqual(10);
}
