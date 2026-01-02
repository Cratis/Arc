// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_observing_complex_data_and_data_changes : given.a_scenario_web_application
{
    ObservableQueryExecutionContext<IEnumerable<ComplexObservableReadModel>>? _executionResult;
    ComplexObservableReadModel[]? _updatedData;

    void Establish()
    {
        LoadQueryProxy<ComplexObservableReadModel>("ObserveComplex");
        _updatedData =
        [
            new ComplexObservableReadModel
            {
                Id = Guid.NewGuid(),
                Name = "Updated Complex Item 1",
                Metadata = new Metadata { CreatedAt = DateTime.UtcNow.AddDays(-1), Tags = ["updated", "tag2", "tag3"] },
                Items =
                [
                    new NestedItem { Key = "key1", Value = "updated_value1" },
                    new NestedItem { Key = "key2", Value = "value2" }
                ]
            },
            new ComplexObservableReadModel
            {
                Id = Guid.NewGuid(),
                Name = "New Complex Item 2",
                Metadata = new Metadata { CreatedAt = DateTime.UtcNow, Tags = ["new", "item"] },
                Items = [new NestedItem { Key = "newkey", Value = "newvalue" }]
            }
        ];
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ComplexObservableReadModel>>(
            "ObserveComplex",
            updateReceiver: ComplexObservableReadModel.UpdateComplexItems);

        // Trigger update and wait for notification
        await _executionResult.UpdateAndWaitAsync(_updatedData);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_received_update() => _executionResult.Updates.ShouldNotBeEmpty();
    [Fact] void should_have_two_items_in_latest_update() => _executionResult.LatestData.Count().ShouldEqual(2);
    [Fact] void should_have_updated_item() => _executionResult.LatestData.Any(_ => _.Name == "Updated Complex Item 1").ShouldBeTrue();
    [Fact] void should_have_new_item() => _executionResult.LatestData.Any(_ => _.Name == "New Complex Item 2").ShouldBeTrue();
    [Fact] void should_have_updated_nested_items() => _executionResult.LatestData.First(_ => _.Name == "Updated Complex Item 1").Items.Count().ShouldEqual(2);
}
