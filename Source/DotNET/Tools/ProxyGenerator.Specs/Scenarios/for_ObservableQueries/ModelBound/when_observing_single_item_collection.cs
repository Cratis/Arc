// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_observing_single_item_collection : given.a_scenario_web_application
{
    ObservableQueryExecutionContext<IEnumerable<ObservableReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<ObservableReadModel>("ObserveSingleItemCollection");

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ObservableReadModel>>("ObserveSingleItemCollection");
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _executionResult.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _executionResult.Result.IsValid.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
    [Fact] void should_have_data_as_collection() => _executionResult.LatestData.ShouldNotBeNull();
    [Fact] void should_have_one_item() => _executionResult.LatestData.Count().ShouldEqual(1);
}
