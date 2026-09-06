// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// Verifies that an <c>IEnumerable&lt;ProductCode&gt;</c> query parameter - a collection of concepts - reaches
/// the server as a query argument and binds to the actual values the caller sent.
/// </summary>
/// <remarks>
/// Asserts against <c>RawJson</c> - the server's response body captured before the JS test harness's
/// instance-shaped deserialization patch runs - rather than <c>Result.Data</c>, because that patch is not written
/// for collection results and reduces an array to an empty object.
/// </remarks>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_query_with_enumerable_concept_parameter_and_checking_data : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<EnumerableParameterReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<EnumerableParameterReadModel>("SearchByCodes");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["codes"] = new[] { "A1", "B2" }
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<EnumerableParameterReadModel>>("SearchByCodes", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_first_code_in_url() => _executionResult.RequestUrl.ShouldContain("codes=A1");
    [Fact] void should_include_second_code_in_url() => _executionResult.RequestUrl.ShouldContain("codes=B2");
    [Fact] void should_return_item_for_first_code() => _executionResult.RawJson.ShouldContain("Product A1");
    [Fact] void should_return_item_for_second_code() => _executionResult.RawJson.ShouldContain("Product B2");
}
