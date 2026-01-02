// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

[Collection(ObservableQueriesCollection.Name)]
public class when_observing_all_items : given.a_scenario_web_application
{
    ObservableQueryExecutionContext<IEnumerable<ObservableReadModel>>? _executionResult;

    void Establish()
    {
        LoadQueryProxy<ObservableReadModel>("ObserveAll", "/tmp/observe-all-proxy.ts");
        File.WriteAllText("/tmp/loadqueryproxy-called.txt", "LoadQueryProxy returned at " + DateTime.Now);
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformObservableQueryViaProxyAsync<IEnumerable<ObservableReadModel>>("ObserveAll");
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_authorized() => _executionResult.Result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _executionResult.Result.IsValid.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();
    [Fact] void should_have_three_items() => _executionResult.LatestData.Count().ShouldEqual(3);
}
