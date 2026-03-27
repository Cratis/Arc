// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_query_with_enumerable_parameter_and_checking_url : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<EnumerableParameterReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<EnumerableParameterReadModel>("SearchByLists");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["names"] = new[] { "Alice", "Bob" },
            ["categories"] = new[] { "Admin", "User" }
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<EnumerableParameterReadModel>>("SearchByLists", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_first_name_in_url() => _executionResult.RequestUrl.ShouldContain("names=Alice");
    [Fact] void should_include_second_name_in_url() => _executionResult.RequestUrl.ShouldContain("names=Bob");
    [Fact] void should_include_first_category_in_url() => _executionResult.RequestUrl.ShouldContain("categories=Admin");
    [Fact] void should_include_second_category_in_url() => _executionResult.RequestUrl.ShouldContain("categories=User");
}
