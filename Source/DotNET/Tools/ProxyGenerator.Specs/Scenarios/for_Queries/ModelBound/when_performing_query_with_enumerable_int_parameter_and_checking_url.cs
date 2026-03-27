// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

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
}
