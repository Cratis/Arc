// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_query_with_route_and_query_parameters : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<ParameterizedReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<ParameterizedReadModel>("GetItemsInCategory");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["category"] = "Electronics",
            ["name"] = "Phone"
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<ParameterizedReadModel>>("GetItemsInCategory", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_category_as_query_parameter() => _executionResult.RequestUrl.ShouldContain("category=Electronics");
    [Fact] void should_include_name_as_query_parameter() => _executionResult.RequestUrl.ShouldContain("name=Phone");
}
