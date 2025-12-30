// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

public class when_observing_single_item_and_data_changes : given.a_scenario_web_application
{
    ObservableQueryExecutionResult<ObservableReadModel>? _executionResult;
    ObservableReadModel? _updatedData;

    void Establish()
    {
        LoadQueryProxy<ObservableReadModel>("ObserveSingle");
        _updatedData = new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66), Name = "Updated Single Item", Value = 84 };
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<ObservableReadModel>("ObserveSingle");

        // Update the data on the backend
        ObservableReadModel.UpdateSingleItem(_updatedData);

        // Sync any new updates from JavaScript
        await Bridge.WaitForWebSocketUpdates(_executionResult);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_received_update() => _executionResult.Updates.ShouldNotBeEmpty();
    [Fact] void should_have_updated_name() => _executionResult.LatestData.Name.ShouldEqual("Updated Single Item");
    [Fact] void should_have_updated_value() => _executionResult.LatestData.Value.ShouldEqual(84);
}
