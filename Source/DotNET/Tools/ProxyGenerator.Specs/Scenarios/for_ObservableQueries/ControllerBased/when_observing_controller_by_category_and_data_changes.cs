// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

[Collection(ObservableQueriesCollection.Name)]
public class when_observing_controller_by_category_and_data_changes : given.a_scenario_web_application
{
    ObservableQueryExecutionResult<IEnumerable<ObservableControllerQueryItem>>? _executionResult;
    ObservableControllerQueryItem[]? _updatedData;

    void Establish()
    {
        LoadControllerQueryProxy<ObservableControllerQueriesController>("ObserveByCategory");
        _updatedData =
        [
            new ObservableControllerQueryItem { Id = Guid.NewGuid(), Name = "Updated Electronics Controller Item 1", Value = 100 },
            new ObservableControllerQueryItem { Id = Guid.NewGuid(), Name = "Updated Electronics Controller Item 2", Value = 200 },
            new ObservableControllerQueryItem { Id = Guid.NewGuid(), Name = "New Electronics Controller Item 3", Value = 300 }
        ];
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ObservableControllerQueryItem>>(
            "ObserveByCategory",
            new { category = "Electronics" });

        // Update the data on the backend via HTTP POST
        await HttpClient.PostAsJsonAsync("/api/observable-controller-queries/update/category/Electronics", _updatedData);

        // Sync observable updates to get fresh HTTP snapshot
        await Bridge.WaitForWebSocketUpdates(_executionResult);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_received_update() => _executionResult.Updates.ShouldNotBeEmpty();
    [Fact] void should_have_three_items_in_latest_update() => _executionResult.LatestData.Count().ShouldEqual(3);
    [Fact] void should_have_new_item() => _executionResult.LatestData.Any(_ => _.Name == "New Electronics Controller Item 3").ShouldBeTrue();
    [Fact] void should_have_updated_values() => _executionResult.LatestData.First(_ => _.Name == "Updated Electronics Controller Item 1").Value.ShouldEqual(100);
}
