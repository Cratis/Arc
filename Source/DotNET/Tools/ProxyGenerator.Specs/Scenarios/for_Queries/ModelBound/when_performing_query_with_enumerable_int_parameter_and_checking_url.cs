// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// Verifies that an <c>IEnumerable&lt;int&gt;</c> query parameter survives proxy generation, reaches the server as
/// a query argument, and binds to the actual values the caller sent.
/// </summary>
/// <remarks>
/// SearchByIds projects one item per id in <c>ids</c> - a correct bind returns three items, a broken bind (the
/// parameter silently classified as an injected, empty <c>IEnumerable&lt;int&gt;</c>) returns zero. Asserting only
/// the URL and <c>IsSuccess</c>, as this spec originally did, is green either way - which is exactly what let the
/// underlying defect ship. The data assertions below are what actually distinguish the two outcomes.
/// <para>
/// They assert against <c>RawJson</c> - the server's response body captured before the JS test harness's
/// instance-shaped deserialization patch runs - rather than <c>Result.Data</c>, because that patch is not written
/// for collection results and reduces an array to an empty object.
/// </para>
/// </remarks>
[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_query_with_enumerable_int_parameter_and_checking_url : given.a_scenario_web_application
{
    QueryExecutionResult<IEnumerable<EnumerableParameterReadModel>>? _executionResult;

    void Establish() => LoadQueryProxy<EnumerableParameterReadModel>("SearchByIds");

    async Task Because()
    {
        var parameters = new Dictionary<string, object>
        {
            ["ids"] = new[] { 1, 2, 3 }
        };

        _executionResult = await Bridge.PerformQueryViaProxyAsync<IEnumerable<EnumerableParameterReadModel>>("SearchByIds", parameters);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_first_id_in_url() => _executionResult.RequestUrl.ShouldContain("ids=1");
    [Fact] void should_include_second_id_in_url() => _executionResult.RequestUrl.ShouldContain("ids=2");
    [Fact] void should_include_third_id_in_url() => _executionResult.RequestUrl.ShouldContain("ids=3");

    [Fact] void should_return_item_for_first_id() => _executionResult.RawJson.ShouldContain("Item 1");
    [Fact] void should_return_item_for_second_id() => _executionResult.RawJson.ShouldContain("Item 2");
    [Fact] void should_return_item_for_third_id() => _executionResult.RawJson.ShouldContain("Item 3");
}
