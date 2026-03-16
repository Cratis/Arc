// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_controller_query_with_enumerable_parameters_and_checking_url : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<ControllerQueryItem>>? _executionResult;

    void Establish() => LoadControllerQueryProxy<ControllerQueriesController>("SearchByLists");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["names"] = new[] { "Alice", "Bob" },
            ["values"] = new[] { 10, 20 }
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<ControllerQueryItem>>("SearchByLists", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_first_name_in_url() => _executionResult.RequestUrl.ShouldContain("names=Alice");
    [Fact] void should_include_second_name_in_url() => _executionResult.RequestUrl.ShouldContain("names=Bob");
    [Fact] void should_include_first_value_in_url() => _executionResult.RequestUrl.ShouldContain("values=10");
    [Fact] void should_include_second_value_in_url() => _executionResult.RequestUrl.ShouldContain("values=20");
}
