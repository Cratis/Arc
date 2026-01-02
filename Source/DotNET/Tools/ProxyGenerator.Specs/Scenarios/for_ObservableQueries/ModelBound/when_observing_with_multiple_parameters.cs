// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

[Collection(ObservableQueriesCollection.Name)]
public class when_observing_with_multiple_parameters : given.a_scenario_web_application
{
    ObservableQueryExecutionContext<IEnumerable<ParameterizedObservableReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<ParameterizedObservableReadModel>("ObserveByNameAndCategory");

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ParameterizedObservableReadModel>>(
            "ObserveByNameAndCategory",
            new { name = "Laptop", category = "Electronics" });
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _executionResult.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _executionResult.Result.IsValid.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
    [Fact] void should_have_correct_name() => _executionResult.LatestData.First().Name.ShouldEqual("Laptop");
    [Fact] void should_have_correct_category() => _executionResult.LatestData.First().Category.ShouldEqual("Electronics");
}
