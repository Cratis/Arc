// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// Verifies that an <c>IEnumerable&lt;string&gt;</c> query parameter reaches the server as a query argument and
/// binds to the actual values the caller sent - a collection of strings shares the same classification defect
/// as a collection of primitives or concepts.
/// </summary>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_query_with_enumerable_string_parameter_and_checking_data : given.a_scenario_web_application
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
    [Fact] void should_pair_the_first_name_with_its_category() => _executionResult.RawJson.ShouldContain("\"name\":\"Alice\",\"category\":\"Admin\"");
    [Fact] void should_pair_the_second_name_with_its_category() => _executionResult.RawJson.ShouldContain("\"name\":\"Bob\",\"category\":\"User\"");
}
