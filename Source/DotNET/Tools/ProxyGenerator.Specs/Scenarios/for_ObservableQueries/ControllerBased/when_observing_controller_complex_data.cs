// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_observing_controller_complex_data : given.a_scenario_web_application
{
    ObservableQueryExecutionContext<IEnumerable<ComplexObservableControllerItem>>? _executionResult;

    void Establish() => LoadControllerQueryProxy<ObservableControllerQueriesController>("ObserveComplex");

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ComplexObservableControllerItem>>("ObserveComplex");
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _executionResult.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _executionResult.Result.IsValid.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
    [Fact] void should_have_metadata() => _executionResult.LatestData.First().Metadata.ShouldNotBeNull();
    [Fact] void should_have_items() => _executionResult.LatestData.First().Items.ShouldNotBeEmpty();
}
