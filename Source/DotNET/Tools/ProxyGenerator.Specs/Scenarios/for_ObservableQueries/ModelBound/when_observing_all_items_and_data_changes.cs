// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

[Collection(ObservableQueriesCollection.Name)]
public class when_observing_all_items_and_data_changes : given.a_scenario_web_application
{
    ObservableQueryExecutionResult<IEnumerable<ObservableReadModel>>? _executionResult;
    ObservableReadModel[]? _updatedData;

    void Establish()
    {
        LoadQueryProxy<ObservableReadModel>("ObserveAll");
        _updatedData =
        [
            new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), Name = "Updated Item 1", Value = 10 },
            new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), Name = "Updated Item 2", Value = 20 },
            new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3), Name = "Updated Item 3", Value = 30 },
            new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4), Name = "New Item 4", Value = 40 }
        ];
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ObservableReadModel>>("ObserveAll");

        // Update the data on the backend
        ObservableReadModel.UpdateAllItems(_updatedData);

        // Sync any new updates from JavaScript
        await Bridge.WaitForWebSocketUpdates(_executionResult);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_received_update() => _executionResult.Updates.ShouldNotBeEmpty();
    [Fact] void should_have_updated_data_in_latest_update() => _executionResult.LatestData.Count().ShouldEqual(4);
    [Fact] void should_have_new_item_in_latest_update() => _executionResult.LatestData.Any(_ => _.Name == "New Item 4").ShouldBeTrue();
    [Fact] void should_have_updated_values() => _executionResult.LatestData.First(_ => _.Name == "Updated Item 1").Value.ShouldEqual(10);
}
