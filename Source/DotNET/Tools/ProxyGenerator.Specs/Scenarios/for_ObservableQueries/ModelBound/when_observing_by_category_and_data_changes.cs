// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

public class when_observing_by_category_and_data_changes : given.a_scenario_web_application
{
    ObservableQueryExecutionResult<IEnumerable<ParameterizedObservableReadModel>>? _executionResult;
    ParameterizedObservableReadModel[]? _updatedData;

    void Establish()
    {
        LoadQueryProxy<ParameterizedObservableReadModel>("ObserveByCategory");
        _updatedData =
        [
            new ParameterizedObservableReadModel { Id = Guid.NewGuid(), Name = "Updated Electronics Item 1", Category = "Electronics", Value = 100 },
            new ParameterizedObservableReadModel { Id = Guid.NewGuid(), Name = "Updated Electronics Item 2", Category = "Electronics", Value = 200 },
            new ParameterizedObservableReadModel { Id = Guid.NewGuid(), Name = "New Electronics Item 3", Category = "Electronics", Value = 300 }
        ];
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ParameterizedObservableReadModel>>(
            "ObserveByCategory",
            new { category = "Electronics" });

        // Wait a bit for the initial connection
        await Task.Delay(100);

        // Update the data on the backend
        ParameterizedObservableReadModel.UpdateItemsForCategory("Electronics", _updatedData);

        // Wait for the update to propagate
        await Task.Delay(200);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_received_update() => _executionResult.Updates.ShouldNotBeEmpty();
    [Fact] void should_have_three_items_in_latest_update() => _executionResult.LatestData.Count().ShouldEqual(3);
    [Fact] void should_have_new_item() => _executionResult.LatestData.Any(_ => _.Name == "New Electronics Item 3").ShouldBeTrue();
    [Fact] void should_have_updated_values() => _executionResult.LatestData.First(_ => _.Name == "Updated Electronics Item 1").Value.ShouldEqual(100);
}
