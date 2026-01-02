// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_observing_controller_all_items : given.a_scenario_web_application
{
    ObservableQueryExecutionContext<IEnumerable<ObservableControllerQueryItem>>? _executionResult;

    void Establish() => LoadControllerQueryProxy<ObservableControllerQueriesController>("ObserveAll");

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ObservableControllerQueryItem>>("ObserveAll");
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _executionResult.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _executionResult.Result.IsValid.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
    [Fact] void should_have_three_items() => _executionResult.LatestData.Count().ShouldEqual(3);
}
