// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

[Collection(ObservableQueriesCollection.Name)]
public class when_observing_controller_by_id : given.a_scenario_web_application
{
    ObservableQueryExecutionContext<ObservableControllerQueryItem>? _executionResult;
    Guid _id;

    void Establish()
    {
        LoadControllerQueryProxy<ObservableControllerQueriesController>("ObserveById");
        _id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x99);
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<ObservableControllerQueryItem>("ObserveById", new { id = _id });
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _executionResult.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _executionResult.Result.IsValid.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
    [Fact] void should_have_correct_id() => _executionResult.LatestData.Id.ShouldEqual(_id);
}
